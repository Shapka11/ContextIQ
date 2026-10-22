using System.Security.Cryptography;
using ContextIQ.Api.Configuration;
using ContextIQ.Api.Data;
using ContextIQ.Api.DTOs;
using ContextIQ.Api.Models;
using ContextIQ.Api.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ContextIQ.Api.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this WebApplication app)
    {
        app.MapPost("/api/sessions/guest", async (AppDbContext db, IOptions<ContextIQ.Api.Configuration.SessionOptions> options, HttpContext ctx, CancellationToken ct) =>
        {
            var user = new User();
            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var expires = DateTime.UtcNow.AddDays(options.Value.LifetimeDays);
            db.Users.Add(user);
            db.UserProfiles.Add(new UserProfile { UserId = user.Id });
            db.GuestSessions.Add(new GuestSession { UserId = user.Id, TokenHash = GuestAuthentication.Hash(token), ExpiresAt = expires });
            await db.SaveChangesAsync(ct);
            ctx.Response.Headers.CacheControl = "no-store";
            return Results.Created("/api/profile", new GuestSessionResponse(user.Id, token, expires));
        }).WithName("CreateGuestSession").WithTags("Sessions").WithSummary("Создать гостевую сессию")
          .WithDescription("Случайный Bearer-токен возвращается один раз; срок действия по умолчанию 30 дней. Тело не требуется.")
          .Produces<GuestSessionResponse>(201).ProducesProblem(429).RequireRateLimiting("sessions");
    }
}
