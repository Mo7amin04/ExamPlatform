using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.AI;

/// <summary>Question content submitted inline for improvement (e.g. unsaved edits in the editor).</summary>
public sealed record QuestionContentInput : IQuestionContent
{
    public string Text { get; init; } = string.Empty;
    public QuestionType Type { get; init; }
    public Difficulty Difficulty { get; init; } = Difficulty.Medium;
    public BloomLevel BloomLevel { get; init; } = BloomLevel.Understand;
    public decimal Points { get; init; } = 1;
    public string? Explanation { get; init; }
    public string? ExpectedAnswer { get; init; }
    public IReadOnlyList<QuestionOptionInput> Options { get; init; } = [];
}

/// <summary>
/// Requests an improved version of a question. Either <see cref="QuestionId"/> (a saved question) or
/// <see cref="CourseId"/> + <see cref="Question"/> (inline content) must be provided.
/// The result is a proposal only; nothing is saved.
/// </summary>
public sealed record ImproveQuestionCommand : IRequest<GeneratedQuestionDto>
{
    public Guid? QuestionId { get; init; }
    public Guid? CourseId { get; init; }
    public Guid? TopicId { get; init; }
    public QuestionContentInput? Question { get; init; }
    public string? Instructions { get; init; }
}

public sealed class ImproveQuestionCommandValidator : AbstractValidator<ImproveQuestionCommand>
{
    public ImproveQuestionCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.QuestionId.HasValue || (x.CourseId.HasValue && x.Question is not null))
            .WithName("Question")
            .WithMessage("Provide either a saved question id, or a course id with the question content.");
        RuleFor(x => x.Question!.Text).NotEmpty().MaximumLength(4000).When(x => x.Question is not null && !x.QuestionId.HasValue);
        RuleFor(x => x.Question!.Type).IsInEnum().When(x => x.Question is not null && !x.QuestionId.HasValue);
        RuleFor(x => x.Instructions).MaximumLength(2000);
    }
}

public sealed class ImproveQuestionCommandHandler(
    IApplicationDbContext db,
    ICourseAccessService access,
    AIGenerationLogger aiLogger,
    IAIQuestionGenerator generator) : IRequestHandler<ImproveQuestionCommand, GeneratedQuestionDto>
{
    public async Task<GeneratedQuestionDto> Handle(ImproveQuestionCommand request, CancellationToken cancellationToken)
    {
        Guid courseId;
        Guid? topicId;
        GeneratedQuestion original;

        if (request.QuestionId is { } questionId)
        {
            var question = await db.Questions
                .AsNoTracking()
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken)
                ?? throw new NotFoundException(nameof(Question), questionId);

            courseId = question.CourseId;
            topicId = question.TopicId;
            original = new GeneratedQuestion
            {
                Text = question.Text,
                Type = question.Type,
                Difficulty = question.Difficulty,
                BloomLevel = question.BloomLevel,
                Points = question.Points,
                Explanation = question.Explanation,
                ExpectedAnswer = question.ExpectedAnswer,
                Options = question.Options.OrderBy(o => o.Order)
                    .Select(o => new QuestionOptionInput(o.Text, o.IsCorrect, o.MatchText))
                    .ToList()
            };
        }
        else
        {
            courseId = request.CourseId!.Value;
            topicId = request.TopicId;
            var q = request.Question!;
            original = new GeneratedQuestion
            {
                Text = q.Text,
                Type = q.Type,
                Difficulty = q.Difficulty,
                BloomLevel = q.BloomLevel,
                Points = q.Points,
                Explanation = q.Explanation,
                ExpectedAnswer = q.ExpectedAnswer,
                Options = q.Options
            };
        }

        await access.EnsureCourseAccessAsync(courseId, cancellationToken);

        var course = await db.Courses.AsNoTracking().FirstAsync(c => c.Id == courseId, cancellationToken);
        var topic = topicId is { } tid
            ? await db.Topics.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tid && t.CourseId == courseId, cancellationToken)
            : null;

        var providerRequest = new QuestionImprovementRequest(
            course.Code, course.Name, topic?.Name, original, request.Instructions?.Trim());

        var (improved, _) = await aiLogger.RunAsync(
            AIOperation.Improve,
            courseId,
            topic?.Id,
            1,
            providerRequest,
            ct => generator.ImproveAsync(providerRequest, ct),
            _ => 1,
            cancellationToken);

        return GeneratedQuestionInspector.Inspect(improved, original.Type);
    }
}
