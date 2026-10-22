using System.Security.Claims;
using ContextIQ.Api.Data;
using ContextIQ.Api.DTOs;
using ContextIQ.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ContextIQ.Api.Endpoints;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/feedback", async (FeedbackRequest input, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (input.Rating is not (-1 or 1) || input.RecommendationId == Guid.Empty)
                throw ApiException.Invalid("recommendationId must be a UUID; rating must be 1 (like) or -1 (dislike).");
            var id = CurrentUser.Id(user);
            if (!await db.Recommendations.AnyAsync(x => x.Id == input.RecommendationId && x.UserId == id, ct))
                throw new ApiException(404, "Recommendation not found", "No recommendation exists for the current user with this ID.");
            var feedbackId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Feedbacks\" (\"Id\", \"UserId\", \"RecommendationId\", \"Rating\", \"CreatedAt\") VALUES ({feedbackId}, {id}, {input.RecommendationId}, {input.Rating}, {now}) ON CONFLICT (\"UserId\", \"RecommendationId\") DO UPDATE SET \"Rating\" = excluded.\"Rating\", \"CreatedAt\" = excluded.\"CreatedAt\"", ct);
            var feedback = await db.Feedbacks.AsNoTracking().SingleAsync(x => x.UserId == id && x.RecommendationId == input.RecommendationId, ct);
            return Results.Ok(new FeedbackResponse(feedback.Id, feedback.RecommendationId, feedback.Rating, feedback.CreatedAt));
        }).WithName("SaveFeedback").WithTags("Feedback").WithSummary("Лайк или дизлайк своей рекомендации")
          .WithDescription("Пример: {\"recommendationId\":\"<UUID>\",\"rating\":1}. Повторный запрос обновляет оценку. Чужой и неизвестный UUID возвращают одинаковый 404.")
          .Produces<FeedbackResponse>().ProducesProblem(400).ProducesProblem(404);
    }
}
