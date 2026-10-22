using ContextIQ.Api.DTOs;
using ContextIQ.Api.Services;

namespace ContextIQ.Api.Endpoints;

public static class AudioEndpoints
{
    public const long MaxAudioBytes = 20 * 1024 * 1024;
    public const long MaxRequestBytes = MaxAudioBytes + 1024 * 1024;

    public static void MapAudioEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/audio/analyze", async (IFormFile file, PythonAiClient python, CancellationToken ct) =>
        {
            if (file.Length > MaxAudioBytes) throw new ApiException(413, "Audio too large", "Maximum WAV size is 20 MiB.");
            if (file.Length < 44 || !Path.GetExtension(file.FileName).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                file.ContentType is not ("audio/wav" or "audio/x-wav" or "audio/wave" or "application/octet-stream"))
                throw ApiException.Invalid("Upload a nonempty PCM .wav file using multipart field 'file'.");
            await using var stream = file.OpenReadStream();
            var header = new byte[12];
            await stream.ReadExactlyAsync(header, ct);
            if (!header.AsSpan(0, 4).SequenceEqual("RIFF"u8) || !header.AsSpan(8, 4).SequenceEqual("WAVE"u8) ||
                (long)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4, 4)) + 8 != file.Length)
                throw ApiException.Invalid("Invalid RIFF/WAVE header or file length.");
            stream.Position = 0;
            return Results.Ok(await python.AnalyzeAudioAsync(stream, ct));
        }).DisableAntiforgery().WithName("AnalyzeAudio").WithTags("Audio").WithSummary("Физический анализ WAV")
          .WithDescription("Multipart поле file: PCM mono/stereo, 8/16/24/32 bit, до 20 MiB и 120 секунд. Возвращает физические характеристики. Требуется Bearer-токен.")
          .Produces<PythonAudioFeatures>().ProducesProblem(400).ProducesProblem(413).ProducesProblem(422).ProducesProblem(502).ProducesProblem(503)
          .RequireRateLimiting("ai");
    }
}
