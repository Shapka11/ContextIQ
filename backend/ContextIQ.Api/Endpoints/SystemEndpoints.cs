using ContextIQ.Api.Data;
using ContextIQ.Api.DTOs;
using ContextIQ.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ContextIQ.Api.Endpoints;

public static class SystemEndpoints
{
    public static void MapSystemEndpoints(this WebApplication app)
    {
        app.MapGet("/api/catalog", async (AppDbContext db, CancellationToken ct) =>
        {
            var items = await db.CatalogItems.AsNoTracking().OrderBy(x => x.SourceId).ToArrayAsync(ct);
            return Results.Ok(items.Select(RecommendationService.ToResponse).ToArray());
        }).WithName("GetCatalog").WithTags("Catalog").WithSummary("Общий каталог рекомендаций")
          .WithDescription("Импорт из ai-service/data/catalog.json. id — UUID БД, sourceId — исходный ID Python.")
          .Produces<CatalogResponse[]>();

        app.MapGet("/api/health", async (AppDbContext db, PythonAiClient python, CancellationToken ct) =>
        {
            var connected = await db.Database.CanConnectAsync(ct);
            PythonHealth? health = null;
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
            budget.CancelAfter(TimeSpan.FromSeconds(3));
            try { health = await python.CheckHealthAsync(budget.Token); }
            catch (ApiException) { }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
            if (!connected || health is null)
                return Results.Problem(statusCode: 503, title: "Service degraded", detail: "A required dependency is unavailable.",
                    extensions: new Dictionary<string, object?> { ["database"] = connected ? "ok" : "unavailable", ["python"] = health });
            return Results.Ok(new HealthResponse("ok", "ok", health));
        }).WithName("GetHealth").WithTags("Health").WithSummary("Состояние backend, БД и Python")
          .WithDescription("200 при доступности всех зависимостей; иначе 503 ProblemDetails. Бюджет проверки Python — 3 секунды.")
          .Produces<HealthResponse>().ProducesProblem(503);
    }
}
