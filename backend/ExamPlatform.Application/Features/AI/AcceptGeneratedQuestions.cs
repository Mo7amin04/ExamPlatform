using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.AI;

/// <summary>A generated question as reviewed (and possibly edited) by the teacher.</summary>
public sealed record AcceptedQuestion : QuestionInput;

public sealed class AcceptedQuestionValidator : QuestionInputValidator<AcceptedQuestion>;

public sealed record AcceptGeneratedQuestionsResult(IReadOnlyList<Guid> QuestionIds);

/// <summary>
/// The only path by which AI output enters the question bank: an explicit teacher confirmation.
/// Accepted questions are stored as drafts with <see cref="QuestionSource.AI"/> so they remain reviewable.
/// </summary>
public sealed record AcceptGeneratedQuestionsCommand : IRequest<AcceptGeneratedQuestionsResult>
{
    public const int MaxQuestions = 50;

    public Guid? GenerationId { get; init; }
    public IReadOnlyList<AcceptedQuestion> Questions { get; init; } = [];
}

public sealed class AcceptGeneratedQuestionsCommandValidator : AbstractValidator<AcceptGeneratedQuestionsCommand>
{
    public AcceptGeneratedQuestionsCommandValidator()
    {
        RuleFor(x => x.Questions).NotEmpty().WithMessage("Select at least one question to accept.")
            .Must(q => q.Count <= AcceptGeneratedQuestionsCommand.MaxQuestions)
            .WithMessage($"At most {AcceptGeneratedQuestionsCommand.MaxQuestions} questions can be accepted at once.");
        RuleForEach(x => x.Questions).SetValidator(new AcceptedQuestionValidator());
    }
}

public sealed class AcceptGeneratedQuestionsCommandHandler(
    IApplicationDbContext db,
    QuestionWriter writer,
    ICourseAccessService access) : IRequestHandler<AcceptGeneratedQuestionsCommand, AcceptGeneratedQuestionsResult>
{
    public async Task<AcceptGeneratedQuestionsResult> Handle(AcceptGeneratedQuestionsCommand request, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        foreach (var item in request.Questions)
        {
            var question = await writer.CreateAsync(item, QuestionSource.AI, cancellationToken);
            ids.Add(question.Id);
        }

        if (request.GenerationId is { } generationId)
        {
            var accessibleCourseIds = access.AccessibleCourseIds();
            var log = await db.AIGenerations.FirstOrDefaultAsync(
                g => g.Id == generationId && accessibleCourseIds.Contains(g.CourseId), cancellationToken);
            if (log is not null)
                log.AcceptedCount += ids.Count;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AcceptGeneratedQuestionsResult(ids);
    }
}
