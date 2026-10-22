using System.Security.Claims;
using ContextIQ.Api.Data;
using ContextIQ.Api.DTOs;
using ContextIQ.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ContextIQ.Api.Endpoints;

public static class CheckInEndpoints
{
    public static void MapCheckInEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/checkins", async (CheckInRequest request, ClaimsPrincipal user, RecommendationService service, CancellationToken ct) =>
            Results.Ok(await service.CheckInAsync(CurrentUser.Id(user), request, ct)))
            .WithName("CreateCheckIn").WithTags("Check-ins").WithSummary("Проанализировать настроение и получить рекомендации")
            .WithDescription("Пример: {\"text\":\"Я устал, хочу отдохнуть\",\"emoji\":\"😴\"}. Анализ и ранжирование выполняет Python; analysisMode показывает demo/openai.")
            .Produces<CheckInResponse>().ProducesProblem(400).ProducesProblem(502).ProducesProblem(503).RequireRateLimiting("ai");

        api.MapGet("/history", async (int? page, int? pageSize, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            var p = page ?? 1;
            var size = pageSize ?? 20;
            if (p is < 1 or > 1000000 || size is < 1 or > 100) throw ApiException.Invalid("page: 1–1000000; pageSize: 1–100.");
            var id = CurrentUser.Id(user);
            var query = db.MoodEntries.AsNoTracking().Where(x => x.UserId == id);
            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Skip((p - 1) * size).Take(size).ToArrayAsync(ct);
            return Results.Ok(new HistoryResponse(p, size, total, items.Select(RecommendationService.ToResponse).ToArray()));
        }).WithName("GetHistory").WithTags("Check-ins").WithSummary("История настроений с пагинацией")
          .WithDescription("От старых записей к новым, сортировка по CreatedAt и UUID; page=1, pageSize=20 по умолчанию.")
          .Produces<HistoryResponse>().ProducesProblem(400);
    }
}
