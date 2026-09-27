using ExamPlatform.Domain.Enums;
using FluentValidation;

namespace ExamPlatform.Application.Features.Questions;

/// <summary>Full editable question payload (content + classification) used by create/update/accept flows.</summary>
public abstract record QuestionInput : IQuestionContent
{
    public Guid CourseId { get; init; }
    public Guid? TopicId { get; init; }
    public string Text { get; init; } = string.Empty;
    public QuestionType Type { get; init; }
    public Difficulty Difficulty { get; init; } = Difficulty.Medium;
    public BloomLevel BloomLevel { get; init; } = BloomLevel.Understand;
    public decimal Points { get; init; } = 1;
    public string? Explanation { get; init; }
    public string? ExpectedAnswer { get; init; }
    public QuestionStatus? Status { get; init; }
    public IReadOnlyList<QuestionOptionInput> Options { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public class QuestionInputValidator<T> : AbstractValidator<T> where T : QuestionInput
{
    public const int MaxTags = 10;

    public QuestionInputValidator()
    {
        Include(new QuestionContentValidator());

        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Difficulty).IsInEnum();
        RuleFor(x => x.BloomLevel).IsInEnum();
        RuleFor(x => x.Explanation).MaximumLength(4000);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Tags).NotNull()
            .Must(t => t.Count <= MaxTags).WithMessage($"A question can have at most {MaxTags} tags.");
        RuleForEach(x => x.Tags).NotEmpty().MaximumLength(50);
    }
}
