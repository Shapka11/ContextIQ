using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContextIQ.Api.DTOs;
using ContextIQ.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ContextIQ.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "catalog.json");
        await using var stream = File.OpenRead(path);
        var catalog = await JsonSerializer.DeserializeAsync<PythonCatalogItem[]>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web), ct)
            ?? throw new InvalidDataException("Catalog is empty.");
        if (catalog.Length == 0 || catalog.Any(i => !PythonContract.Catalog(i)) ||
            catalog.Select(i => i.Id).Distinct().Count() != catalog.Length)
            throw new InvalidDataException("Catalog does not match the Python contract.");
        var existing = await db.CatalogItems.ToDictionaryAsync(x => x.SourceId, ct);
        foreach (var item in catalog)
        {
            if (!existing.TryGetValue(item.Id, out var entity))
            {
                entity = new CatalogItem { Id = CatalogId(item.Id), SourceId = item.Id };
                db.CatalogItems.Add(entity);
            }
            entity.Title = item.Title;
            entity.Category = item.Category;
            entity.Tags = item.Tags;
            entity.MoodTags = item.Moods;
            entity.Energy = item.Energy;
        }
        await db.SaveChangesAsync(ct);
    }

    public static Guid CatalogId(string sourceId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("ContextIQ/catalog/" + sourceId))[..16];
        bytes[7] = (byte)((bytes[7] & 0x0f) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        return new Guid(bytes);
    }
}
