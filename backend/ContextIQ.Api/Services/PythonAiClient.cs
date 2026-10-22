using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextIQ.Api.Configuration;
using ContextIQ.Api.DTOs;
using Microsoft.Extensions.Options;

namespace ContextIQ.Api.Services;

public sealed class PythonAiClient(HttpClient http, IOptions<PythonAiOptions> options, ILogger<PythonAiClient> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private const int MaxResponseBytes = 1024 * 1024;

    public async Task<AnalysisResult> AnalyzeMoodAsync(PythonAnalyzeRequest input, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/analyze") { Content = JsonContent.Create(input, options: Json) };
        var (analysis, mode) = await SendAsync<PythonAnalysis>(request, ct);
        if (!PythonContract.Analysis(analysis) || mode is not ("demo" or "openai")) throw ApiException.Upstream();
        return new(analysis, mode);
    }

    public async Task<PythonRankedItem[]> RankRecommendationsAsync(PythonRankRequest input, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/rank") { Content = JsonContent.Create(input, options: Json) };
        var (items, _) = await SendAsync<PythonRankedItem[]>(request, ct);
        var ids = input.Candidates.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        if (items.Length != Math.Min(5, input.Candidates.Length))
        {
            throw ApiException.Upstream();
        }

        var returnedIds = new HashSet<string>(StringComparer.Ordinal);
        var previousScore = double.PositiveInfinity;

        foreach (var item in items)
        {
            if (item is null || !ids.Contains(item.Id) || !returnedIds.Add(item.Id))
            {
                throw ApiException.Upstream();
            }

            if (!PythonContract.Unit(item.Score) || item.Score > previousScore ||
                string.IsNullOrWhiteSpace(item.Explanation) || item.Explanation.Length > 2000)
            {
                throw ApiException.Upstream();
            }

            if (item.Components is null ||
                !PythonContract.Unit(item.Components.MoodMatch) ||
                !PythonContract.Unit(item.Components.PreferenceMatch) ||
                !PythonContract.Unit(item.Components.ContextMatch) ||
                !PythonContract.Unit(item.Components.Novelty))
            {
                throw ApiException.Upstream();
            }

            previousScore = item.Score;
        }

        return items;
    }

    public async Task<PythonAudioFeatures> AnalyzeAudioAsync(Stream wav, CancellationToken ct)
    {
        using var multipart = new MultipartFormDataContent();
        var content = new StreamContent(wav);
        content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        multipart.Add(content, "file", "audio.wav");
        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/audio-features") { Content = multipart };
        var (a, _) = await SendAsync<PythonAudioFeatures>(request, ct, audio: true);
        if (!double.IsFinite(a.DurationSeconds) || a.DurationSeconds is <= 0 or > 120 || a.SampleRate is < 1 or > 192000 ||
            !PythonContract.Unit(a.Rms) || !PythonContract.Unit(a.PeakAmplitude) || a.Rms > a.PeakAmplitude + 1e-9 ||
            !double.IsFinite(a.DominantFrequencyHz) || a.DominantFrequencyHz < 0 || a.DominantFrequencyHz > a.SampleRate / 2.0 ||
            !double.IsFinite(a.SpectralCentroidHz) || a.SpectralCentroidHz < 0 || a.SpectralCentroidHz > a.SampleRate / 2.0)
            throw ApiException.Upstream();
        return a;
    }

    public async Task<PythonHealth> CheckHealthAsync(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "health");
        var (health, _) = await SendAsync<PythonHealth>(request, ct);
        if (health.Status != "ok" || health.TextAnalysisMode is not ("demo" or "openai") || string.IsNullOrWhiteSpace(health.Version))
            throw ApiException.Upstream();
        return health;
    }

    private async Task<(T Value, string Mode)> SendAsync<T>(HttpRequestMessage request, CancellationToken ct, bool audio = false)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Python endpoint {Endpoint} returned HTTP {Status}", request.RequestUri, (int)response.StatusCode);
                if (audio && response.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.RequestEntityTooLarge)
                    throw new ApiException((int)response.StatusCode, "Invalid audio", "Python rejected the WAV file. Use PCM mono/stereo, up to 120 seconds and 20 MiB.");
                if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests) throw ApiException.Unavailable();
                throw ApiException.Upstream();
            }
            if (response.Content.Headers.ContentLength > MaxResponseBytes ||
                response.Content.Headers.ContentType?.MediaType != "application/json") throw ApiException.Upstream();
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var bytes = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(bytes, timeout.Token)) > 0)
            {
                if (buffer.Length + read > MaxResponseBytes) throw ApiException.Upstream();
                await buffer.WriteAsync(bytes.AsMemory(0, read), timeout.Token);
            }
            var result = JsonSerializer.Deserialize<T>(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), Json);
            if (result is null) throw ApiException.Upstream();
            var mode = response.Headers.TryGetValues("X-Analysis-Mode", out var modes) ? modes.First() : "";
            return (result, mode);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Python endpoint {Endpoint} timed out", request.RequestUri);
            throw ApiException.Unavailable();
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Python endpoint {Endpoint} could not be reached", request.RequestUri);
            throw ApiException.Unavailable();
        }
        catch (IOException)
        {
            logger.LogWarning("Python response stream failed for {Endpoint}", request.RequestUri);
            throw ApiException.Unavailable();
        }
        catch (JsonException)
        {
            logger.LogWarning("Python endpoint {Endpoint} returned incompatible JSON", request.RequestUri);
            throw ApiException.Upstream();
        }
    }
}
