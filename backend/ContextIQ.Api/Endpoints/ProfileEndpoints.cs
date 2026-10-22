using System.Security.Claims;
using ContextIQ.Api.Data;
using ContextIQ.Api.DTOs;
using ContextIQ.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ContextIQ.Api.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/profile", async (ClaimsPrincipal user, ProfileService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(CurrentUser.Id(user), ct)))
            .WithName("GetProfile").WithTags("Profile").WithSummary("Предпочтения текущего пользователя")
            .WithDescription("Профиль определяется по Bearer-токену.").Produces<ProfileResponse>().ProducesProblem(404);

        api.MapPatch("/profile", async (UpdateProfileRequest input, ClaimsPrincipal user, ProfileService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateAsync(CurrentUser.Id(user), input, ct)))
            .WithName("UpdateProfile").WithTags("Profile").WithSummary("Обновить предпочтения")
            .WithDescription("Непереданные или null поля сохраняются, [] очищает поле. Пример: {\"favoriteGenres\":[\"relax\"],\"favoriteCategories\":[\"music\"]}.")
            .Produces<ProfileResponse>().ProducesProblem(400).ProducesProblem(404);

        api.MapDelete("/profile", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            var id = CurrentUser.Id(user);
            await db.Users.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        }).WithName("DeleteProfile").WithTags("Profile").WithSummary("Удалить все персональные данные")
          .WithDescription("Каскадно удаляет пользователя, профиль, сессии, историю, рекомендации и отзывы. Токен сразу становится недействительным.")
          .Produces(204);
    }
}
