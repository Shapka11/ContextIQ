using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ContextIQ.Api.Configuration;
using ContextIQ.Api.Data;
using ContextIQ.Api.Endpoints;
using ContextIQ.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.Configure<KeyManagementOptions>(o =>
{
    o.XmlRepository = new VolatileXmlRepository();
    o.XmlEncryptor = new NullXmlEncryptor();
});

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = AudioEndpoints.MaxRequestBytes);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = AudioEndpoints.MaxRequestBytes);
builder.Services.AddOptions<PythonAiOptions>()
    .BindConfiguration("PythonAi")
    .ValidateDataAnnotations()
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" &&
        string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment), "Use an absolute HTTP(S) URL without credentials or query.")
    .ValidateOnStart();
builder.Services.AddOptions<ContextIQ.Api.Configuration.SessionOptions>()
    .BindConfiguration("Sessions")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<PythonAiClient>((sp, http) =>
{
    var options = sp.GetRequiredService<IOptions<PythonAiOptions>>().Value;
    http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    http.Timeout = Timeout.InfiniteTimeSpan;
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AllowAutoRedirect = false,
    UseCookies = false,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
});

var provider = builder.Configuration["Database:Provider"] ?? "PostgreSQL";
var connection = builder.Configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("Database connection string is required.");
if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<SqliteAppDbContext>(o => o.UseSqlite(connection));
    builder.Services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<SqliteAppDbContext>());
}
else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<PostgresAppDbContext>(o => o.UseNpgsql(connection));
    builder.Services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<PostgresAppDbContext>());
}
else
{
    throw new InvalidOperationException("Database:Provider must be PostgreSQL or Sqlite.");
}

builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<RecommendationService>();
builder.Services.AddAuthentication(GuestAuthentication.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, GuestAuthentication>(GuestAuthentication.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await Results.Problem(statusCode: 429, title: "Too many requests", detail: "Please wait before trying again.").ExecuteAsync(context.HttpContext);
    };
    o.AddPolicy("sessions", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("ai", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(o =>
{
    o.OperationFilter<GuestSecurityOperationFilter>();
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ContextIQ API",
        Version = "v1",
        Description = "Guest Bearer sessions, mood history and Python recommendations. Create a session, then use Authorize."
    });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Paste the token returned by POST /api/sessions/guest."
    });
    o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();
app.UseExceptionHandler(errorApp => errorApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var status = ex switch
    {
        ApiException a => a.StatusCode,
        BadHttpRequestException b => b.StatusCode,
        _ => 500
    };
    var title = ex switch
    {
        ApiException a => a.Title,
        BadHttpRequestException => "Invalid request",
        _ => "Internal server error"
    };
    var detail = ex switch
    {
        ApiException a => a.Message,
        BadHttpRequestException => "Check the JSON, query parameters, content type or multipart file size.",
        _ => "The request could not be completed. Contact support with the traceId."
    };
    if (status >= 500)
    {
        app.Logger.LogWarning("Request failed: {ErrorType}, HTTP {Status}, trace {TraceId}", ex?.GetType().Name, status, ctx.TraceIdentifier);
    }

    await Results.Problem(statusCode: status, title: title, detail: detail).ExecuteAsync(ctx);
}));
app.UseStatusCodePages(async status =>
    await Results.Problem(statusCode: status.HttpContext.Response.StatusCode).ExecuteAsync(status.HttpContext));
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        ctx.Response.Headers.CacheControl = "no-store";
    }

    await next(ctx);
});
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "ContextIQ API v1"));
}
app.MapSessionEndpoints();
app.MapSystemEndpoints();
var personal = app.MapGroup("/api").RequireAuthorization().ProducesProblem(401).ProducesProblem(403);
personal.MapProfileEndpoints();
personal.MapCheckInEndpoints();
personal.MapAudioEndpoints();
personal.MapFeedbackEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
    {
        await db.Database.MigrateAsync();
    }

    await DbSeeder.SeedAsync(db);
}
await app.RunAsync();

public partial class Program;
