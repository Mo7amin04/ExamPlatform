using ExamPlatform.Domain.Enums;
using FluentValidation;

namespace ExamPlatform.Application.Features.Questions;

public sealed record QuestionOptionInput(string Text, bool IsCorrect, string? MatchText = null);

/// <summary>
/// The answerable content of a question. Shared by manual questions, AI-generated questions and
/// AI improvement results so that every source is validated by exactly the same rules.
/// </summary>
public interface IQuestionContent
{
    string Text { get; }
    QuestionType Type { get; }
    decimal Points { get; }
    string? ExpectedAnswer { get; }
    IReadOnlyList<QuestionOptionInput> Options { get; }
}

public static class QuestionTypeRules
{
    public static bool UsesOptions(QuestionType type) =>
        type is QuestionType.MultipleChoice or QuestionType.MultipleSelect or QuestionType.TrueFalse
            or QuestionType.Matching or QuestionType.Ordering;

    public static bool RequiresExpectedAnswer(QuestionType type) =>
        type is QuestionType.ShortAnswer or QuestionType.FillBlank;
}

public sealed class QuestionContentValidator : AbstractValidator<IQuestionContent>
{
    public const int MaxOptions = 10;
    public const int MaxMatchingPairs = 20;

    public QuestionContentValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage("Question text is required.").MaximumLength(4000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Points)
            .GreaterThan(0).WithMessage("Points must be greater than zero.")
            .LessThanOrEqualTo(1000);
        RuleFor(x => x.ExpectedAnswer).MaximumLength(4000);
        RuleFor(x => x.Options).NotNull();

        RuleForEach(x => x.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.Text).NotEmpty().WithMessage("Option text is required.").MaximumLength(1000);
            option.RuleFor(o => o.MatchText).MaximumLength(1000);
        }).When(x => x.Options is not null);

        RuleFor(x => x.ExpectedAnswer)
            .NotEmpty().WithMessage("An expected answer is required for this question type.")
            .When(x => QuestionTypeRules.RequiresExpectedAnswer(x.Type));

        RuleFor(x => x.Options)
            .Must(o => o.Count == 0).WithMessage("This question type does not use options.")
            .When(x => x.Options is not null && !QuestionTypeRules.UsesOptions(x.Type));

        RuleFor(x => x.Options)
            .Must(HaveUniqueTexts).WithMessage("Options must not contain duplicates.")
            .When(x => x.Options is not null && QuestionTypeRules.UsesOptions(x.Type));

        When(x => x.Options is not null && x.Type == QuestionType.MultipleChoice, () =>
        {
            RuleFor(x => x.Options.Count).InclusiveBetween(2, MaxOptions)
                .OverridePropertyName(nameof(IQuestionContent.Options))
                .WithMessage($"Multiple choice questions need between 2 and {MaxOptions} options.");
            RuleFor(x => x.Options.Count(o => o.IsCorrect)).Equal(1)
                .OverridePropertyName(nameof(IQuestionContent.Options))
                .WithMessage("Multiple choice questions must have exactly one correct option.");
        });

        When(x => x.Options is not null && x.Type == QuestionType.MultipleSelect, () =>
        {
            RuleFor(x => x.Options.Count).InclusiveBetween(2, MaxOptions)
                .OverridePropertyName(nameof(IQuestionContent.Options))
                .WithMessage($"Multiple select questions need between 2 and {MaxOptions} options.");
            RuleFor(x => x.Options.Count(o => o.IsCorrect)).GreaterThanOrEqualTo(1)
                .OverridePropertyName(nameof(IQuestionContent.Options))
                .WithMessage("Multiple select questions must have at least one correct option.");
        });

        When(x => x.Options is not null && x.Type == QuestionType.TrueFalse, () =>
        {
            RuleFor(x => x.Options.Count).Equal(2)
                .OverridePropertyName(nameof(IQuestionContent.Options))
                .WithMessage("True/false questions must have exactly two options.");
            RuleFor(x => x.Options.Count(o => o.IsCorrect)).Equal(1)
                .OverridePropertyName(nameof(IQuestionContent.Options))
                .WithMessage("True/false questions must have exactly one correct option.");
        });

        When(x => x.Options is not null && x.Type == QuestionType.Matching, () =>
        {
            RuleFor(x => x.Options.Count).InclusiveBetween(2, MaxMatchingPairs)
                .OverridePropertyName(nameof(IQuestionContent.Options))
                .WithMessage($"Matching questions need between 2 and {MaxMatchingPairs} pairs.");
            RuleFor(x => x.Options)
                .Must(o => o.All(p => !string.IsNullOrWhiteSpace(p.MatchText)))
                .WithMessage("Every matching pair needs a matching item.")
                .Must(o => HaveUnique(o.Select(p => p.MatchText)))
                .WithMessage("Matching items must not contain duplicates.");
        });

        When(x => x.Options is not null && x.Type == QuestionType.Ordering, () =>
        {
            RuleFor(x => x.Options.Count).InclusiveBetween(2, MaxMatchingPairs)
                .OverridePropertyName(nameof(IQuestionContent.Options))
                .WithMessage($"Ordering questions need between 2 and {MaxMatchingPairs} items.");
        });
    }

    private static bool HaveUniqueTexts(IReadOnlyList<QuestionOptionInput> options) =>
        HaveUnique(options.Select(o => o.Text));

    private static bool HaveUnique(IEnumerable<string?> values)
    {
        var normalized = values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim().ToLowerInvariant()).ToList();
        return normalized.Count == normalized.Distinct().Count();
    }
}
