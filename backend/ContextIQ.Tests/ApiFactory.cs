using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContextIQ.Api.Data;
using ContextIQ.Api.DTOs;
using ContextIQ.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ContextIQ.Tests;

public sealed class PythonStub : HttpMessageHandler
{
    public bool Offline { get; set; }
    public bool UnknownRankId { get; set; }
    public bool RejectAudio { get; set; }
    public PythonRankRequest? LastRank { get; private set; }
    public List<string> Paths { get; } = [];
    public static readonly PythonAnalysis Analysis = new("stressed", 0.25, -0.4, ["relax"], 0.8);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Paths.Add(request.RequestUri!.AbsolutePath);
        if (Offline) throw new HttpRequestException("simulated offline; secret should never reach caller");
        HttpResponseMessage response;
        switch (request.RequestUri.AbsolutePath)
        {
            case "/health":
                response = Json(new PythonHealth("ok", "demo", "1.0.0"));
                break;
            case "/internal/analyze":
                var body = await request.Content!.ReadFromJsonAsync<PythonAnalyzeRequest>(ct);
                Assert.NotNull(body);
                Assert.True(!string.IsNullOrWhiteSpace(body.Text) || !string.IsNullOrWhiteSpace(body.Emoji));
                response = Json(Analysis);
                response.Headers.Add("X-Analysis-Mode", "demo");
                break;
            case "/internal/rank":
                LastRank = await request.Content!.ReadFromJsonAsync<PythonRankRequest>(ct);
                Assert.NotNull(LastRank);
                Assert.All(LastRank.Candidates, c => Assert.True(PythonContract.Catalog(c)));
                var items = LastRank.Candidates.Take(5).Select((c, index) => new PythonRankedItem(
                    UnknownRankId ? "unrequested-id" : c.Id, 0.9 - index * 0.01, new(1, 0.5, 0.8, 1), "Deterministic test ranking")).ToArray();
                response = Json(items);
                break;
            case "/internal/audio-features":
                Assert.StartsWith("multipart/form-data", request.Content!.Headers.ContentType!.ToString());
                var bytes = await request.Content.ReadAsByteArrayAsync(ct);
                Assert.Contains("RIFF", System.Text.Encoding.Latin1.GetString(bytes));
                response = RejectAudio ? new(HttpStatusCode.UnprocessableEntity) { Content = JsonContent.Create(new { detail = "secret" }) }
                    : Json(new PythonAudioFeatures(1, 8000, 0.35, 0.5, 440, 440));
                break;
            default: throw new InvalidOperationException(request.RequestUri.ToString());
        }
        return response;
    }
    public static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(AppContext.BaseDirectory, $"test-{Guid.NewGuid():N}.db");
    public PythonStub Python { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SqliteAppDbContext>>();
            services.RemoveAll<SqliteAppDbContext>();
            services.AddDbContext<SqliteAppDbContext>(o => o.UseSqlite($"Data Source={databasePath};Foreign Keys=True"));
            services.AddHttpClient<PythonAiClient>().ConfigurePrimaryHttpMessageHandler(() => Python);
        });
    }

    public async Task WithDb(Func<AppDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" }) File.Delete(databasePath + suffix);
    }
}
