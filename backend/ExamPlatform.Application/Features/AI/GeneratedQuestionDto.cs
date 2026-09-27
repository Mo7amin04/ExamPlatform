using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Application.Features.AI;

/// <summary>
/// A generated (not persisted) question returned for teacher review. <see cref="Issues"/> lists every rule the
/// AI output violates; invalid questions must be fixed before they can be accepted.
/// </summary>
public sealed record GeneratedQuestionDto(
    string Text,
    QuestionType Type,
    Difficulty Difficulty,
    BloomLevel BloomLevel,
    decimal Points,
    string? Explanation,
    string? ExpectedAnswer,
    IReadOnlyList<QuestionOptionInput> Options,
    bool IsValid,
    IReadOnlyList<string> Issues);

public static class GeneratedQuestionInspector
{
    private static readonly QuestionContentValidator Validator = new();

    /// <summary>Validates AI output with the same rules used for manually authored questions.</summary>
    public static GeneratedQuestionDto Inspect(GeneratedQuestion question, QuestionType? expectedType = null)
    {
        var issues = Validator.Validate(question).Errors.Select(e => e.ErrorMessage).Distinct().ToList();

        if (expectedType is { } type && question.Type != type)
            issues.Add($"Expected a {type} question but the AI returned {question.Type}.");

        return new GeneratedQuestionDto(
            question.Text?.Trim() ?? string.Empty,
            question.Type,
            question.Difficulty,
            question.BloomLevel,
            question.Points,
            question.Explanation?.Trim(),
            question.ExpectedAnswer?.Trim(),
            question.Options ?? [],
            issues.Count == 0,
            issues);
    }
}
