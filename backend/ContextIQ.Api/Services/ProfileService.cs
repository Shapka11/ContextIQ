using ContextIQ.Api.Data;
using ContextIQ.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ContextIQ.Api.Services;

public sealed class ProfileService(AppDbContext db)
{
    public async Task<ProfileResponse> GetAsync(Guid userId, CancellationToken ct)
    {
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new ApiException(404, "Profile not found", "The guest profile does not exist.");
        return new(userId, profile.FavoriteGenres, profile.DislikedGenres, profile.FavoriteCategories, profile.UpdatedAt);
    }

    public async Task<ProfileResponse> UpdateAsync(Guid userId, UpdateProfileRequest input, CancellationToken ct)
    {
        if (input.FavoriteGenres is null && input.DislikedGenres is null && input.FavoriteCategories is null)
            throw ApiException.Invalid("Provide at least one preference field; an empty array clears that field.");
        if ((input.FavoriteGenres is not null && !PythonContract.Tags(input.FavoriteGenres, 50)) ||
            (input.DislikedGenres is not null && !PythonContract.Tags(input.DislikedGenres, 50)) ||
            (input.FavoriteCategories is not null && (input.FavoriteCategories.Length > 4 || input.FavoriteCategories.Any(x => !PythonContract.Categories.Contains(x)))))
            throw ApiException.Invalid("Genres: up to 50 nonempty tags of at most 100 characters. Categories: movie, music, book, activity.");
        var p = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new ApiException(404, "Profile not found", "The guest profile does not exist.");
        var favorite = input.FavoriteGenres is null ? p.FavoriteGenres : Normalize(input.FavoriteGenres);
        var disliked = input.DislikedGenres is null ? p.DislikedGenres : Normalize(input.DislikedGenres);
        if (favorite.Intersect(disliked).Any()) throw ApiException.Invalid("Favorite and disliked genres must not overlap.");
        p.FavoriteGenres = favorite;
        p.DislikedGenres = disliked;
        if (input.FavoriteCategories is not null) p.FavoriteCategories = input.FavoriteCategories.Distinct().ToArray();
        p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new(userId, p.FavoriteGenres, p.DislikedGenres, p.FavoriteCategories, p.UpdatedAt);
    }

    private static string[] Normalize(string[] values) => values.Select(x => x.Trim().ToLowerInvariant()).Distinct().ToArray();
}
