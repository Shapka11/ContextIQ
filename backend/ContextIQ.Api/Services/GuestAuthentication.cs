using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using ContextIQ.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContextIQ.Api.Services;

public sealed class GuestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, AppDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "GuestBearer";
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header)) return AuthenticateResult.NoResult();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.Fail("Bearer token required.");
        var token = header[7..];
        if (token.Length != 43 || token.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_')))
            return AuthenticateResult.Fail("Invalid session.");
        var hash = Hash(token);
        var now = DateTime.UtcNow;
        var userId = await db.GuestSessions.Where(s => s.TokenHash == hash && s.ExpiresAt > now)
            .Join(db.Users, s => s.UserId, u => u.Id, (s, u) => (Guid?)u.Id).SingleOrDefaultAsync(Context.RequestAborted);
        if (userId is null) return AuthenticateResult.Fail("Invalid or expired session.");
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())], SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Bearer";
        await Results.Problem(statusCode: 401, title: "Session required", detail: "Provide a valid guest session using Authorization: Bearer <token>.").ExecuteAsync(Context);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        Results.Problem(statusCode: 403, title: "Access denied").ExecuteAsync(Context);
}

public static class CurrentUser
{
    public static Guid Id(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
