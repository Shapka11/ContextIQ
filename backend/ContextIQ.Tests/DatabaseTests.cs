using ContextIQ.Api.Data;
using ContextIQ.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ContextIQ.Tests;

public sealed class DatabaseTests
{
    [Fact]
    public async Task CatalogSeed_IsIdempotentAndPreservesPythonIds()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await app.WithDb(async db =>
        {
            await DbSeeder.SeedAsync(db);
            await DbSeeder.SeedAsync(db);
            Assert.Equal(32, await db.CatalogItems.CountAsync());
            var item = await db.CatalogItems.SingleAsync(i => i.SourceId == "music-01");
            Assert.Equal(DbSeeder.CatalogId("music-01"), item.Id);
            Assert.Null(item.ExternalUrl);
            Assert.False(db.Database.HasPendingModelChanges());
        });
    }

    [Fact]
    public async Task Database_RejectsRecommendationAttachedToOtherUsersMood()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await app.WithDb(async db =>
        {
            var owner = new User();
            var other = new User();
            var mood = new MoodEntry { UserId = owner.Id, DetectedMood = "neutral", AnalysisMode = "demo" };
            db.Users.AddRange(owner, other);
            db.MoodEntries.Add(mood);
            await db.SaveChangesAsync();
            db.Recommendations.Add(new Recommendation
            {
                UserId = other.Id,
                MoodEntryId = mood.Id,
                CatalogItemId = await db.CatalogItems.Select(i => i.Id).FirstAsync(),
                Explanation = "test",
                Score = 0.5
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task Database_RejectsFeedbackAttachedToOtherUsersRecommendation()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        await app.WithDb(async db =>
        {
            var owner = new User();
            var other = new User();
            var mood = new MoodEntry { UserId = owner.Id, DetectedMood = "neutral", AnalysisMode = "demo" };
            db.Users.AddRange(owner, other);
            db.MoodEntries.Add(mood);
            var rec = new Recommendation
            {
                UserId = owner.Id,
                MoodEntryId = mood.Id,
                CatalogItemId = await db.CatalogItems.Select(i => i.Id).FirstAsync(),
                Explanation = "test",
                Score = 0.5
            };
            db.Recommendations.Add(rec);
            await db.SaveChangesAsync();
            db.Feedbacks.Add(new Feedback { UserId = other.Id, RecommendationId = rec.Id, Rating = 1 });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
    }

    [Fact]
    public void PostgreSqlMigration_GeneratesProviderSpecificSqlWithoutPendingChanges()
    {
        using var db = new PostgresAppDbContext(new DbContextOptionsBuilder<PostgresAppDbContext>()
            .UseNpgsql("Host=localhost;Database=not-connected;Username=test;Password=test").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("uuid NOT NULL", script);
        Assert.Contains("timestamp with time zone", script);
        Assert.Contains("ON DELETE CASCADE", script);
        Assert.Contains("CK_Feedback_Rating", script);
    }
}
