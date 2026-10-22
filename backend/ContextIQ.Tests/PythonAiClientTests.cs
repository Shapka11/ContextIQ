using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ContextIQ.Api.Configuration;
using ContextIQ.Api.DTOs;
using ContextIQ.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ContextIQ.Tests;

public sealed class PythonAiClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => send(r, ct);
    }
    private static PythonAiClient Client(HttpMessageHandler handler, int seconds = 2) => new(
        new HttpClient(handler) { BaseAddress = new("http://python.test/"), Timeout = Timeout.InfiniteTimeSpan },
        Options.Create(new PythonAiOptions { TimeoutSeconds = seconds }), NullLogger<PythonAiClient>.Instance);
    private static HttpResponseMessage Response(string json, bool withMode = true)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        if (withMode) response.Headers.Add("X-Analysis-Mode", "demo");
        return response;
    }
    private const string ValidAnalysis = "{\"mood\":\"sad\",\"energy\":0.2,\"valence\":-0.5,\"themes\":[\"relax\"],\"confidence\":0.8}";

    [Fact]
    public async Task Analyze_SendsOnlyCamelCaseContractAndReadsMode()
    {
        var client = Client(new Handler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/internal/analyze", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadFromJsonAsync<JsonElement>(ct);
            Assert.Equal("Привет", json.GetProperty("text").GetString());
            Assert.Equal(2, json.EnumerateObject().Count());
            return Response(ValidAnalysis);
        }));
        var analysis = await client.AnalyzeMoodAsync(new("Привет", "🙂"), default);
        Assert.Equal("sad", analysis.Analysis.Mood);
        Assert.Equal("demo", analysis.Mode);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{broken")]
    [InlineData("{\"mood\":\"sad\",\"energy\":2,\"valence\":0,\"themes\":[],\"confidence\":0.8}")]
    [InlineData("{\"mood\":\"unknown\",\"energy\":0.2,\"valence\":0,\"themes\":[],\"confidence\":0.8}")]
    [InlineData("{\"mood\":\"sad\",\"energy\":0.2,\"valence\":0,\"themes\":null,\"confidence\":0.8}")]
    [InlineData("{\"mood\":\"sad\",\"energy\":0.2,\"valence\":0,\"themes\":[null],\"confidence\":0.8}")]
    [InlineData("{\"mood\":\"sad\",\"energy\":0.2,\"valence\":0,\"themes\":[],\"confidence\":0.8,\"secret\":\"ignored?\"}")]
    public async Task Analyze_RejectsInvalidResponses(string json)
    {
        var client = Client(new Handler((_, _) => Task.FromResult(Response(json))));
        var ex = await Assert.ThrowsAsync<ApiException>(() => client.AnalyzeMoodAsync(new("test", ""), default));
        Assert.Equal(502, ex.StatusCode);
    }

    [Fact]
    public async Task Analyze_RequiresExplicitProviderMode()
    {
        var client = Client(new Handler((_, _) => Task.FromResult(Response(ValidAnalysis, false))));
        Assert.Equal(502, (await Assert.ThrowsAsync<ApiException>(() => client.AnalyzeMoodAsync(new("test", ""), default))).StatusCode);
    }

    [Theory]
    [InlineData(400, 502)]
    [InlineData(422, 502)]
    [InlineData(429, 503)]
    [InlineData(500, 503)]
    [InlineData(502, 503)]
    public async Task HttpFailures_MapToSafeErrors(int upstreamStatus, int expectedStatus)
    {
        var client = Client(new Handler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)upstreamStatus)
        { Content = new StringContent("secret provider details") })));
        var error = await Assert.ThrowsAsync<ApiException>(() => client.CheckHealthAsync(default));
        Assert.Equal(expectedStatus, error.StatusCode);
        Assert.DoesNotContain("secret", error.Message);
    }

    [Fact]
    public async Task NetworkFailure_Returns503()
    {
        var client = Client(new Handler((_, _) => throw new HttpRequestException("private endpoint")));
        Assert.Equal(503, (await Assert.ThrowsAsync<ApiException>(() => client.CheckHealthAsync(default))).StatusCode);
    }

    [Fact]
    public async Task Timeout_Returns503_AndCallerCancellationPropagates()
    {
        var client = Client(new Handler(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); throw new InvalidOperationException(); }), 1);
        Assert.Equal(503, (await Assert.ThrowsAsync<ApiException>(() => client.CheckHealthAsync(default))).StatusCode);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CheckHealthAsync(cancellation.Token));
    }

    [Fact]
    public async Task Rank_UsesOriginalIdsAndValidatesScoresAndComponents()
    {
        var client = Client(new Handler(async (request, ct) =>
        {
            Assert.Equal("/internal/rank", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadFromJsonAsync<JsonElement>(ct);
            Assert.True(json.GetProperty("preferences").TryGetProperty("seenIds", out _));
            var candidate = json.GetProperty("candidates")[0];
            Assert.Equal("music-01", candidate.GetProperty("id").GetString());
            Assert.False(candidate.TryGetProperty("sourceId", out _));
            Assert.False(candidate.TryGetProperty("externalUrl", out _));
            return PythonStub.Json(new[] { new PythonRankedItem("music-01", 0.8, new(1, 0.5, 0.5, 1), "explanation") });
        }));
        var result = await client.RankRecommendationsAsync(RankRequest(), default);
        Assert.Equal(0.8, Assert.Single(result).Score);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("[{\"id\":\"unknown\",\"score\":0.8,\"components\":{\"moodMatch\":1,\"preferenceMatch\":1,\"contextMatch\":1,\"novelty\":1},\"explanation\":\"x\"}]")]
    [InlineData("[{\"id\":\"music-01\",\"score\":9,\"components\":{\"moodMatch\":1,\"preferenceMatch\":1,\"contextMatch\":1,\"novelty\":1},\"explanation\":\"x\"}]")]
    [InlineData("[{\"id\":\"music-01\",\"score\":0.8,\"components\":null,\"explanation\":\"x\"}]")]
    public async Task Rank_RejectsIncompatibleResponses(string body)
    {
        var client = Client(new Handler((_, _) => Task.FromResult(Response(body))));
        Assert.Equal(502, (await Assert.ThrowsAsync<ApiException>(() => client.RankRecommendationsAsync(RankRequest(), default))).StatusCode);
    }

    [Fact]
    public async Task Audio_UsesMultipartAndReadsPhysicalFeatures()
    {
        var client = Client(new PythonStub());
        using var wav = new MemoryStream("RIFF test"u8.ToArray());
        var features = await client.AnalyzeAudioAsync(wav, default);
        Assert.Equal(440, features.DominantFrequencyHz);
        Assert.Equal(8000, features.SampleRate);
    }

    [Fact]
    public async Task Audio_RejectsMissingOrImpossibleFeatures()
    {
        var client = Client(new Handler((_, _) => Task.FromResult(Response("{\"durationSeconds\":1,\"sampleRate\":8000,\"rms\":0.5,\"peakAmplitude\":0.1,\"dominantFrequencyHz\":440,\"spectralCentroidHz\":440}"))));
        using var stream = new MemoryStream();
        Assert.Equal(502, (await Assert.ThrowsAsync<ApiException>(() => client.AnalyzeAudioAsync(stream, default))).StatusCode);
    }

    [Fact]
    public async Task OversizedResponse_IsRejected()
    {
        var client = Client(new Handler((_, _) => Task.FromResult(Response(new string(' ', 1024 * 1024 + 1)))));
        Assert.Equal(502, (await Assert.ThrowsAsync<ApiException>(() => client.CheckHealthAsync(default))).StatusCode);
    }

    private static PythonRankRequest RankRequest() => new(PythonStub.Analysis, new([], [], []),
        [new("music-01", "Music", "music", ["relax"], ["stressed"], 0.2)]);
}
