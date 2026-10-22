using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ContextIQ.Api.DTOs;
using ContextIQ.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ContextIQ.Tests;

public sealed class ApiTests
{
    private static async Task<GuestSessionResponse> Guest(HttpClient client)
    {
        using var response = await client.PostAsync("/api/sessions/guest", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<GuestSessionResponse>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.Token);
        return session;
    }
    private static async Task<CheckInResponse> CheckIn(HttpClient client, string text = "Я устал")
    {
        using var response = await client.PostAsJsonAsync("/api/checkins", new { text, emoji = "😴" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CheckInResponse>())!;
    }
    private static async Task Problem(HttpResponseMessage response, HttpStatusCode code)
    {
        Assert.Equal(code, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)code, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("secret", body.ToString());
    }

    [Fact]
    public async Task GuestSession_CreatesUserProfileAndHashedToken()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var guest = await Guest(client);
        Assert.NotEqual(Guid.Empty, guest.UserId);
        Assert.Equal(43, guest.Token.Length);
        Assert.True(guest.ExpiresAt > DateTime.UtcNow.AddDays(29));
        await app.WithDb(async db =>
        {
            Assert.Equal(1, await db.Users.CountAsync());
            Assert.Equal(guest.UserId, (await db.UserProfiles.SingleAsync()).UserId);
            Assert.Equal(GuestAuthentication.Hash(guest.Token), (await db.GuestSessions.SingleAsync()).TokenHash);
            Assert.Equal(32, await db.CatalogItems.CountAsync());
            Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
        });
    }

    [Fact]
    public async Task Profile_PatchNormalizesAndPreservesOmittedFields()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var guest = await Guest(client);
        var empty = await client.GetFromJsonAsync<ProfileResponse>("/api/profile");
        Assert.Equal(guest.UserId, empty!.UserId);
        Assert.Empty(empty.FavoriteGenres);
        (await client.PatchAsJsonAsync("/api/profile", new { favoriteGenres = new[] { " Relax ", "relax" }, favoriteCategories = new[] { "music" } })).EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync("/api/profile", new { dislikedGenres = new[] { "rock" } })).EnsureSuccessStatusCode();
        var profile = (await client.GetFromJsonAsync<ProfileResponse>("/api/profile"))!;
        Assert.Equal(["relax"], profile.FavoriteGenres);
        Assert.Equal(["music"], profile.FavoriteCategories);
        Assert.Equal(["rock"], profile.DislikedGenres);
        (await client.PatchAsJsonAsync("/api/profile", new { favoriteGenres = Array.Empty<string>() })).EnsureSuccessStatusCode();
        Assert.Empty((await client.GetFromJsonAsync<ProfileResponse>("/api/profile"))!.FavoriteGenres);
    }

    [Fact]
    public async Task CheckIn_UsesPythonContractAndPersistsMoodAndRecommendations()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var guest = await Guest(client);
        (await client.PatchAsJsonAsync("/api/profile", new { favoriteGenres = new[] { "relax" }, dislikedGenres = new[] { "rock" }, favoriteCategories = new[] { "music" } })).EnsureSuccessStatusCode();
        var result = await CheckIn(client);
        Assert.Equal("stressed", result.MoodEntry.Analysis.Mood);
        Assert.Equal("demo", result.MoodEntry.AnalysisMode);
        Assert.Equal(DateTimeKind.Utc, result.MoodEntry.CreatedAt.Kind);
        Assert.Equal(5, result.Recommendations.Length);
        Assert.Equal(["music"], app.Python.LastRank!.Preferences.Categories);
        Assert.Equal(["relax"], app.Python.LastRank.Preferences.Tags);
        Assert.DoesNotContain(app.Python.LastRank.Candidates, c => c.Tags.Contains("rock"));
        Assert.Contains(app.Python.LastRank.Candidates, c => c.Id == "music-01");
        await app.WithDb(async db =>
        {
            var mood = await db.MoodEntries.SingleAsync();
            Assert.Equal(guest.UserId, mood.UserId);
            Assert.Equal("Я устал", mood.Text);
            Assert.Equal(5, await db.Recommendations.CountAsync(x => x.MoodEntryId == mood.Id && x.UserId == guest.UserId));
            Assert.All(await db.Recommendations.ToArrayAsync(), r => Assert.InRange(r.Score, 0, 1));
        });
        await CheckIn(client, "Ещё запись");
        Assert.Equal(5, app.Python.LastRank.Preferences.SeenIds.Length);
    }

    [Fact]
    public async Task History_IsChronologicalPaginatedAndPrivate()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await Guest(client);
        var first = await CheckIn(client, "first");
        await CheckIn(client, "second");
        var page = (await client.GetFromJsonAsync<HistoryResponse>("/api/history?page=1&pageSize=1"))!;
        Assert.Equal(2, page.Total);
        Assert.Equal(first.MoodEntry.Id, Assert.Single(page.Items).Id);
        var next = (await client.GetFromJsonAsync<HistoryResponse>("/api/history?page=2&pageSize=1"))!;
        Assert.Equal("second", Assert.Single(next.Items).Text);
        using var other = app.CreateClient();
        await Guest(other);
        Assert.Empty((await other.GetFromJsonAsync<HistoryResponse>("/api/history"))!.Items);
        Assert.Empty((await client.GetFromJsonAsync<HistoryResponse>("/api/history?page=10"))!.Items);
    }

    [Fact]
    public async Task Feedback_UpsertsAndRejectsOtherUsersRecommendation()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await Guest(client);
        var result = await CheckIn(client);
        var id = result.Recommendations[0].Id;
        (await client.PostAsJsonAsync("/api/feedback", new { recommendationId = id, rating = 1 })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/feedback", new { recommendationId = id, rating = -1 })).EnsureSuccessStatusCode();
        await app.WithDb(async db => Assert.Equal(-1, (await db.Feedbacks.SingleAsync()).Rating));
        using var other = app.CreateClient();
        await Guest(other);
        await Problem(await other.PostAsJsonAsync("/api/feedback", new { recommendationId = id, rating = 1 }), HttpStatusCode.NotFound);
        await Problem(await other.PostAsJsonAsync("/api/feedback", new { recommendationId = Guid.NewGuid(), rating = 1 }), HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api/profile")]
    [InlineData("/api/history")]
    public async Task PersonalEndpoint_RequiresSession_NotUserId(string path)
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await Problem(await client.GetAsync(path + "?userId=" + Guid.NewGuid()), HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new("Bearer", new string('a', 43));
        await Problem(await client.GetAsync(path), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredOrDeletedSession_IsUnauthorized()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await Guest(client);
        await app.WithDb(async db => { var s = await db.GuestSessions.SingleAsync(); s.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); await db.SaveChangesAsync(); });
        await Problem(await client.GetAsync("/api/profile"), HttpStatusCode.Unauthorized);
        var second = await Guest(client);
        await app.WithDb(db => db.Users.Where(u => u.Id == second.UserId).ExecuteDeleteAsync());
        await Problem(await client.GetAsync("/api/profile"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProfile_CascadesAllPersonalDataAndRevokesToken()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await Guest(client);
        var rec = (await CheckIn(client)).Recommendations[0];
        (await client.PostAsJsonAsync("/api/feedback", new { recommendationId = rec.Id, rating = 1 })).EnsureSuccessStatusCode();
        using var other = app.CreateClient();
        var otherGuest = await Guest(other);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/profile")).StatusCode);
        await Problem(await client.GetAsync("/api/profile"), HttpStatusCode.Unauthorized);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/profile")).StatusCode);
        await app.WithDb(async db =>
        {
            Assert.Equal(otherGuest.UserId, (await db.Users.SingleAsync()).Id);
            Assert.Equal(1, await db.UserProfiles.CountAsync());
            Assert.Equal(1, await db.GuestSessions.CountAsync());
            Assert.Empty(await db.MoodEntries.ToArrayAsync());
            Assert.Empty(await db.Recommendations.ToArrayAsync());
            Assert.Empty(await db.Feedbacks.ToArrayAsync());
            Assert.Equal(32, await db.CatalogItems.CountAsync());
        });
    }

    [Theory]
    [InlineData("POST", "/api/checkins", "{}")]
    [InlineData("POST", "/api/checkins", "{\"text\":\"   \"}")]
    [InlineData("POST", "/api/checkins", "{\"text\":\"hi\",\"userId\":\"spoofed\"}")]
    [InlineData("POST", "/api/checkins", "{broken")]
    [InlineData("PATCH", "/api/profile", "{}")]
    [InlineData("PATCH", "/api/profile", "{\"favoriteGenres\":[null]}")]
    [InlineData("PATCH", "/api/profile", "{\"favoriteCategories\":[\"invalid\"]}")]
    [InlineData("PATCH", "/api/profile", "{\"favoriteGenres\":[\"rock\"],\"dislikedGenres\":[\"ROCK\"]}")]
    [InlineData("POST", "/api/feedback", "{\"recommendationId\":\"00000000-0000-0000-0000-000000000001\",\"rating\":0}")]
    public async Task InvalidJsonOrValues_ReturnConsistentProblem(string method, string path, string json)
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await Guest(client);
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        await Problem(await client.SendAsync(request), HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?page=abc")]
    public async Task InvalidPagination_Returns400(string query)
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await Guest(client);
        await Problem(await client.GetAsync("/api/history" + query), HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(false, 503)]
    [InlineData(true, 502)]
    public async Task FailedPython_DoesNotPersistPartialCheckIn(bool unknownId, int status)
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await Guest(client);
        app.Python.Offline = !unknownId;
        app.Python.UnknownRankId = unknownId;
        await Problem(await client.PostAsJsonAsync("/api/checkins", new { text = "test" }), (HttpStatusCode)status);
        await app.WithDb(async db => { Assert.Equal(0, await db.MoodEntries.CountAsync()); Assert.Equal(0, await db.Recommendations.CountAsync()); });
    }

    [Fact]
    public async Task Audio_ForwardsWavAndMapsUpstreamValidation()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await Guest(client);
        using var multipart = Wav();
        var result = await client.PostAsync("/api/audio/analyze", multipart);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(440, (await result.Content.ReadFromJsonAsync<PythonAudioFeatures>())!.DominantFrequencyHz);
        app.Python.RejectAudio = true;
        using var rejected = Wav();
        await Problem(await client.PostAsync("/api/audio/analyze", rejected), HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData("audio.mp3", 44, false, 400)]
    [InlineData("audio.wav", 44, false, 400)]
    [InlineData("audio.wav", 20 * 1024 * 1024 + 1, true, 413)]
    public async Task Audio_RejectsInvalidOrOversizedFile(string name, int length, bool validHeader, int status)
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await Guest(client);
        using var content = Wav(name, length, validHeader);
        await Problem(await client.PostAsync("/api/audio/analyze", content), (HttpStatusCode)status);
        Assert.DoesNotContain("/internal/audio-features", app.Python.Paths);
    }

    [Fact]
    public async Task SwaggerCatalogHealthAndCors_AreAvailable()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        Assert.Equal(32, (await client.GetFromJsonAsync<CatalogResponse[]>("/api/catalog"))!.Length);
        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health");
        Assert.Equal("demo", health!.Python!.TextAnalysisMode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.True(swagger.GetProperty("paths").TryGetProperty("/api/audio/analyze", out _));
        Assert.Equal(0, swagger.GetProperty("paths").GetProperty("/api/sessions/guest").GetProperty("post").GetProperty("security").GetArrayLength());
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/profile");
        preflight.Headers.Add("Origin", "http://localhost:5173");
        preflight.Headers.Add("Access-Control-Request-Method", "PATCH");
        preflight.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        var cors = await client.SendAsync(preflight);
        Assert.Equal("http://localhost:5173", Assert.Single(cors.Headers.GetValues("Access-Control-Allow-Origin")));
        app.Python.Offline = true;
        await Problem(await client.GetAsync("/api/health"), HttpStatusCode.ServiceUnavailable);
    }

    public static MultipartFormDataContent Wav(string name = "audio.wav", int length = 44, bool validHeader = true)
    {
        var bytes = new byte[length];
        if (validHeader)
        {
            "RIFF"u8.CopyTo(bytes);
            BitConverter.GetBytes((uint)(length - 8)).CopyTo(bytes, 4);
            "WAVE"u8.CopyTo(bytes.AsSpan(8));
        }
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(file, "file", name);
        return content;
    }
}
