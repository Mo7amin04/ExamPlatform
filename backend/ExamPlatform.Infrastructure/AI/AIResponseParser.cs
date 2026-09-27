using System.Text.Json;
using System.Text.Json.Serialization;
using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Infrastructure.AI;

/// <summary>
/// Converts raw model output into <see cref="GeneratedQuestion"/>s. Parsing is tolerant (code fences, casing,
/// numeric strings); semantic validation happens afterwards in the Application layer.
/// </summary>
public static class AIResponseParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static IReadOnlyList<GeneratedQuestion> Parse(string? raw, QuestionType fallbackType)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new AIProviderException("The AI provider returned an empty response.");

        var json = StripCodeFences(raw);

        RawResponse? response;
        try
        {
            response = json.TrimStart().StartsWith('[')
                ? new RawResponse { Questions = JsonSerializer.Deserialize<List<RawQuestion>>(json, Options) }
                : JsonSerializer.Deserialize<RawResponse>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new AIProviderException("The AI provider returned malformed JSON.", innerException: ex);
        }

        if (response?.Questions is not { Count: > 0 } questions)
            throw new AIProviderException("The AI provider did not return any questions.");

        return questions.Where(q => q is not null).Select(q => Map(q, fallbackType)).ToList();
    }

    private static GeneratedQuestion Map(RawQuestion raw, QuestionType fallbackType) => new()
    {
        Text = raw.Text?.Trim() ?? string.Empty,
        Type = ParseEnum(raw.Type, fallbackType),
        Difficulty = ParseEnum(raw.Difficulty, Difficulty.Medium),
        BloomLevel = ParseEnum(raw.BloomLevel, BloomLevel.Understand),
        Points = raw.Points is > 0 ? Math.Round(raw.Points.Value, 2) : 1,
        Explanation = string.IsNullOrWhiteSpace(raw.Explanation) ? null : raw.Explanation.Trim(),
        ExpectedAnswer = string.IsNullOrWhiteSpace(raw.ExpectedAnswer) ? null : raw.ExpectedAnswer.Trim(),
        Options = (raw.Options ?? [])
            .Where(o => o is not null)
            .Select(o => new QuestionOptionInput(
                o.Text?.Trim() ?? string.Empty,
                o.IsCorrect,
                string.IsNullOrWhiteSpace(o.MatchText) ? null : o.MatchText.Trim()))
            .ToList()
    };

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        var normalized = value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
        return Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : fallback;
    }

    private static string StripCodeFences(string raw)
    {
        var text = raw.Trim();
        if (!text.StartsWith("```"))
            return text;

        var firstNewLine = text.IndexOf('\n');
        var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewLine >= 0 && lastFence > firstNewLine
            ? text[(firstNewLine + 1)..lastFence].Trim()
            : text.Trim('`');
    }

    private sealed class RawResponse
    {
        public List<RawQuestion>? Questions { get; set; }
    }

    private sealed class RawQuestion
    {
        public string? Text { get; set; }
        public string? Type { get; set; }
        public string? Difficulty { get; set; }
        public string? BloomLevel { get; set; }
        public decimal? Points { get; set; }
        public string? Explanation { get; set; }
        public string? ExpectedAnswer { get; set; }
        public List<RawOption>? Options { get; set; }
    }

    private sealed class RawOption
    {
        public string? Text { get; set; }
        public bool IsCorrect { get; set; }
        public string? MatchText { get; set; }
    }
}
