using System.ComponentModel.DataAnnotations;

namespace ContextIQ.Api.Configuration;

public sealed class PythonAiOptions
{
    [Required, Url]
    public string BaseUrl { get; set; } = "http://localhost:8000";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class SessionOptions
{
    [Range(1, 365)]
    public int LifetimeDays { get; set; } = 30;
}
