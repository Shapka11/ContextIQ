namespace ContextIQ.Api.DTOs;

public sealed record GuestSessionResponse(Guid UserId, string Token, DateTime ExpiresAt);
public sealed record ProfileResponse(Guid UserId, string[] FavoriteGenres, string[] DislikedGenres, string[] FavoriteCategories, DateTime UpdatedAt);
public sealed record UpdateProfileRequest(string[]? FavoriteGenres = null, string[]? DislikedGenres = null, string[]? FavoriteCategories = null);
public sealed record CheckInRequest(string? Text = "", string? Emoji = "");
public sealed record CatalogResponse(Guid Id, string SourceId, string Title, string Category, string[] Tags, double Energy, string[] MoodTags, string? ExternalUrl);
public sealed record RecommendationResponse(Guid Id, CatalogResponse Item, double Score, string Explanation);
public sealed record MoodResponse(Guid Id, string Text, string Emoji, PythonAnalysis Analysis, string AnalysisMode, DateTime CreatedAt);
public sealed record CheckInResponse(MoodResponse MoodEntry, RecommendationResponse[] Recommendations);
public sealed record HistoryResponse(int Page, int PageSize, int Total, MoodResponse[] Items);
public sealed record FeedbackRequest(Guid RecommendationId, int Rating);
public sealed record FeedbackResponse(Guid Id, Guid RecommendationId, int Rating, DateTime CreatedAt);
public sealed record HealthResponse(string Status, string Database, PythonHealth? Python);
