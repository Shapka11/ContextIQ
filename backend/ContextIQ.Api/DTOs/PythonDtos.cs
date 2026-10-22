using System.Text.Json.Serialization;

namespace ContextIQ.Api.DTOs;

public sealed record PythonAnalyzeRequest(string Text, string Emoji);
public sealed record PythonAnalysis(
    [property: JsonRequired] string Mood,
    [property: JsonRequired] double Energy,
    [property: JsonRequired] double Valence,
    [property: JsonRequired] string[] Themes,
    [property: JsonRequired] double Confidence);
public sealed record PythonCatalogItem(
    [property: JsonRequired] string Id,
    [property: JsonRequired] string Title,
    [property: JsonRequired] string Category,
    [property: JsonRequired] string[] Tags,
    [property: JsonRequired] string[] Moods,
    [property: JsonRequired] double Energy);
public sealed record PythonPreferences(string[] Categories, string[] Tags, string[] SeenIds);
public sealed record PythonRankRequest(PythonAnalysis Analysis, PythonPreferences Preferences, PythonCatalogItem[] Candidates);
public sealed record PythonComponents(
    [property: JsonRequired] double MoodMatch,
    [property: JsonRequired] double PreferenceMatch,
    [property: JsonRequired] double ContextMatch,
    [property: JsonRequired] double Novelty);
public sealed record PythonRankedItem(
    [property: JsonRequired] string Id,
    [property: JsonRequired] double Score,
    [property: JsonRequired] PythonComponents Components,
    [property: JsonRequired] string Explanation);
public sealed record PythonAudioFeatures(
    [property: JsonRequired] double DurationSeconds,
    [property: JsonRequired] int SampleRate,
    [property: JsonRequired] double Rms,
    [property: JsonRequired] double PeakAmplitude,
    [property: JsonRequired] double DominantFrequencyHz,
    [property: JsonRequired] double SpectralCentroidHz);
public sealed record PythonHealth(
    [property: JsonRequired] string Status,
    [property: JsonRequired] string TextAnalysisMode,
    [property: JsonRequired] string Version);
public sealed record AnalysisResult(PythonAnalysis Analysis, string Mode);

public static class PythonContract
{
    public static readonly string[] Moods = ["happy", "sad", "stressed", "excited", "neutral"];
    public static readonly string[] Categories = ["movie", "music", "book", "activity"];
    public static bool Unit(double v) => double.IsFinite(v) && v is >= 0 and <= 1;

    public static bool Tags(string[]? tags, int max) =>
        tags is not null && tags.Length <= max &&
        tags.All(t => !string.IsNullOrWhiteSpace(t) && t.Length <= 100);

    public static bool Analysis(PythonAnalysis? a) =>
        a is not null &&
        Moods.Contains(a.Mood) &&
        Unit(a.Energy) &&
        double.IsFinite(a.Valence) && a.Valence is >= -1 and <= 1 &&
        Unit(a.Confidence) &&
        Tags(a.Themes, 30);

    public static bool Catalog(PythonCatalogItem? i) =>
        i is not null &&
        !string.IsNullOrWhiteSpace(i.Id) && i.Id.Length <= 100 &&
        !string.IsNullOrWhiteSpace(i.Title) && i.Title.Length <= 300 &&
        Categories.Contains(i.Category) &&
        Tags(i.Tags, 50) &&
        i.Moods is { Length: >= 1 and <= 5 } && i.Moods.All(Moods.Contains) &&
        Unit(i.Energy);
}
