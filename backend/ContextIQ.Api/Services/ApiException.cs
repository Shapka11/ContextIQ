namespace ContextIQ.Api.Services;

public sealed class ApiException(int statusCode, string title, string detail) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public static ApiException Invalid(string detail) => new(400, "Invalid request", detail);
    public static ApiException Upstream() => new(502, "Invalid AI response", "Python AI returned an invalid or incompatible response.");
    public static ApiException Unavailable() => new(503, "AI service unavailable", "Python AI is unavailable or timed out. Please try again later.");
}
