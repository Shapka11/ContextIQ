using System.Text.Json;
using ContextIQ.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ContextIQ.Api.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<GuestSession> GuestSessions => Set<GuestSession>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<MoodEntry> MoodEntries => Set<MoodEntry>();
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>().HasKey(x => x.Id);
        model.Entity<GuestSession>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasIndex(x => x.ExpiresAt);
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<UserProfile>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId).IsUnique();
            b.HasOne<User>().WithOne().HasForeignKey<UserProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<MoodEntry>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasAlternateKey(x => new { x.Id, x.UserId });
            b.HasIndex(x => new { x.UserId, x.CreatedAt, x.Id });
            b.Property(x => x.Text).HasMaxLength(8000).IsRequired();
            b.Property(x => x.Emoji).HasMaxLength(100).IsRequired();
            b.Property(x => x.DetectedMood).HasMaxLength(20).IsRequired();
            b.Property(x => x.AnalysisMode).HasMaxLength(20).IsRequired();
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            b.ToTable(t => t.HasCheckConstraint("CK_Mood_Ranges", "\"Energy\" >= 0 AND \"Energy\" <= 1 AND \"Valence\" >= -1 AND \"Valence\" <= 1 AND \"Confidence\" >= 0 AND \"Confidence\" <= 1"));
        });
        model.Entity<CatalogItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.SourceId).HasMaxLength(100).IsRequired();
            b.HasIndex(x => x.SourceId).IsUnique();
            b.HasIndex(x => x.Category);
            b.Property(x => x.Title).HasMaxLength(300).IsRequired();
            b.Property(x => x.Category).HasMaxLength(20).IsRequired();
            b.Property(x => x.ExternalUrl).HasMaxLength(2048);
            b.ToTable(t => t.HasCheckConstraint("CK_Catalog_Energy", "\"Energy\" >= 0 AND \"Energy\" <= 1"));
        });
        model.Entity<Recommendation>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasAlternateKey(x => new { x.Id, x.UserId });
            b.HasIndex(x => new { x.UserId, x.CreatedAt });
            b.HasIndex(x => new { x.MoodEntryId, x.CatalogItemId }).IsUnique();
            b.Property(x => x.Explanation).HasMaxLength(2000).IsRequired();
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<MoodEntry>().WithMany().HasForeignKey(x => new { x.MoodEntryId, x.UserId })
                .HasPrincipalKey(x => new { x.Id, x.UserId }).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.CatalogItem).WithMany().HasForeignKey(x => x.CatalogItemId).OnDelete(DeleteBehavior.Restrict);
            b.ToTable(t => t.HasCheckConstraint("CK_Recommendation_Score", "\"Score\" >= 0 AND \"Score\" <= 1"));
        });
        model.Entity<Feedback>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.UserId, x.RecommendationId }).IsUnique();
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Recommendation>().WithMany().HasForeignKey(x => new { x.RecommendationId, x.UserId })
                .HasPrincipalKey(x => new { x.Id, x.UserId }).OnDelete(DeleteBehavior.Cascade);
            b.ToTable(t => t.HasCheckConstraint("CK_Feedback_Rating", "\"Rating\" IN (-1, 1)"));
        });

        var converter = new ValueConverter<string[], string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null)!);
        var comparer = new ValueComparer<string[]>(
            (a, b) => a!.SequenceEqual(b!),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToArray());
        foreach (var entity in model.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(string[]))
                {
                    property.SetValueConverter(converter);
                    property.SetValueComparer(comparer);
                    property.IsNullable = false;
                }
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                        v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
            }
    }
}

public sealed class SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : AppDbContext(options);
public sealed class PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options) : AppDbContext(options);
