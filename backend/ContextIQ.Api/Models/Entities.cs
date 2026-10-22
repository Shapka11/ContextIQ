namespace ContextIQ.Api.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class GuestSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}

public sealed class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string[] FavoriteGenres { get; set; } = [];
    public string[] DislikedGenres { get; set; } = [];
    public string[] FavoriteCategories { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class MoodEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Text { get; set; } = "";
    public string Emoji { get; set; } = "";
    public string DetectedMood { get; set; } = "";
    public double Energy { get; set; }
    public double Valence { get; set; }
    public string[] Themes { get; set; } = [];
    public double Confidence { get; set; }
    public string AnalysisMode { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CatalogItem
{
    public Guid Id { get; set; }
    public string SourceId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public double Energy { get; set; }
    public string[] MoodTags { get; set; } = [];
    public string? ExternalUrl { get; set; }
}

public sealed class Recommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid MoodEntryId { get; set; }
    public Guid CatalogItemId { get; set; }
    public CatalogItem CatalogItem { get; set; } = null!;
    public double Score { get; set; }
    public string Explanation { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Feedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid RecommendationId { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
