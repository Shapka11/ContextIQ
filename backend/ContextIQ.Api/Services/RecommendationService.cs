using ContextIQ.Api.Data;
using ContextIQ.Api.DTOs;
using ContextIQ.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ContextIQ.Api.Services;

public sealed class RecommendationService(AppDbContext db, PythonAiClient python)
{
    public async Task<CheckInResponse> CheckInAsync(Guid userId, CheckInRequest request, CancellationToken ct)
    {
        var text = request.Text ?? "";
        var emoji = request.Emoji ?? "";
        if (text.Length > 8000 || emoji.Length > 100 || (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(emoji)))
            throw ApiException.Invalid("Provide text (up to 8000 characters) or emoji (up to 100 characters).");
        var profile = await db.UserProfiles.AsNoTracking().SingleAsync(x => x.UserId == userId, ct);
        var result = await python.AnalyzeMoodAsync(new(text, emoji), ct);
        var catalog = await db.CatalogItems.OrderBy(x => x.SourceId).Take(1000).ToListAsync(ct);
        var candidates = catalog.Where(i => !i.Tags.Any(t => profile.DislikedGenres.Contains(t.Trim().ToLowerInvariant()))).ToArray();
        var seenIds = await db.Recommendations.Where(x => x.UserId == userId)
            .Select(x => x.CatalogItem.SourceId).Distinct().OrderBy(x => x).Take(10000).ToArrayAsync(ct);
        var ranked = await python.RankRecommendationsAsync(new(result.Analysis,
            new(profile.FavoriteCategories, profile.FavoriteGenres, seenIds), candidates.Select(i =>
                new PythonCatalogItem(i.SourceId, i.Title, i.Category, i.Tags, i.MoodTags, i.Energy)).ToArray()), ct);
        var byId = candidates.ToDictionary(x => x.SourceId, StringComparer.Ordinal);
        var mood = new MoodEntry
        {
            UserId = userId,
            Text = text,
            Emoji = emoji,
            DetectedMood = result.Analysis.Mood,
            Energy = result.Analysis.Energy,
            Valence = result.Analysis.Valence,
            Themes = result.Analysis.Themes,
            Confidence = result.Analysis.Confidence,
            AnalysisMode = result.Mode
        };
        var recommendations = ranked.Select(r => new Recommendation
        {
            UserId = userId,
            MoodEntryId = mood.Id,
            CatalogItemId = byId[r.Id].Id,
            CatalogItem = byId[r.Id],
            Score = r.Score,
            Explanation = r.Explanation,
            CreatedAt = mood.CreatedAt
        }).ToArray();
        db.MoodEntries.Add(mood);
        db.Recommendations.AddRange(recommendations);
        await db.SaveChangesAsync(ct);
        return new(ToResponse(mood), recommendations.Select(r => new RecommendationResponse(r.Id, ToResponse(r.CatalogItem), r.Score, r.Explanation)).ToArray());
    }

    public static MoodResponse ToResponse(MoodEntry m) => new(m.Id, m.Text, m.Emoji,
        new(m.DetectedMood, m.Energy, m.Valence, m.Themes, m.Confidence), m.AnalysisMode, m.CreatedAt);
    public static CatalogResponse ToResponse(CatalogItem i) => new(i.Id, i.SourceId, i.Title, i.Category, i.Tags, i.Energy, i.MoodTags, i.ExternalUrl);
}
