namespace ExamPlatform.Infrastructure.AI;

/// <summary>
/// AI provider configuration bound from the "AI" section. The API key must be supplied through user-secrets or
/// environment variables (AI__ApiKey) and is never committed.
/// </summary>
public sealed class AISettings
{
    public const string SectionName = "AI";

    public const string GeminiProvider = "Gemini";

    /// <summary>Development-only provider returning deterministic sample questions without calling any service.</summary>
    public const string MockProvider = "Mock";

    public string Provider { get; set; } = GeminiProvider;
    public string Model { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";
    public int TimeoutSeconds { get; set; } = 90;
    public double Temperature { get; set; } = 0.7;

    public const string DefaultGeminiModel = "gemini-2.5-flash";
}
