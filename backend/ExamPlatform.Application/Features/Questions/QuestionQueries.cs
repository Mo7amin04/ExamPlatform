using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Models;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Questions;

// ---------- GetQuestions ----------

/// <summary>
/// Server-side paginated, filtered search over the question bank.
/// Archived questions are excluded unless <see cref="Status"/> explicitly asks for them.
/// </summary>
public sealed record GetQuestionsQuery : PagedQuery, IRequest<PagedResult<QuestionListItemDto>>
{
    public Guid? CourseId { get; init; }
    public Guid? TopicId { get; init; }
    public QuestionType? Type { get; init; }
    public Difficulty? Difficulty { get; init; }
    public BloomLevel? BloomLevel { get; init; }
    public QuestionStatus? Status { get; init; }
    public QuestionSource? Source { get; init; }
    public string? Search { get; init; }

    /// <summary>Hide questions already used in this exam (used by the exam builder picker).</summary>
    public Guid? ExcludeExamId { get; init; }
}

public sealed class GetQuestionsQueryValidator : AbstractValidator<GetQuestionsQuery>
{
    public GetQuestionsQueryValidator()
    {
        this.AddPagingRules();
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x.Difficulty).IsInEnum().When(x => x.Difficulty.HasValue);
        RuleFor(x => x.BloomLevel).IsInEnum().When(x => x.BloomLevel.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}

public sealed class GetQuestionsQueryHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<GetQuestionsQuery, PagedResult<QuestionListItemDto>>
{
    public async Task<PagedResult<QuestionListItemDto>> Handle(GetQuestionsQuery request, CancellationToken cancellationToken)
    {
        var accessibleCourseIds = access.AccessibleCourseIds();
        var query = db.Questions.AsNoTracking().Where(q => accessibleCourseIds.Contains(q.CourseId));

        if (request.CourseId is { } courseId) query = query.Where(q => q.CourseId == courseId);
        if (request.TopicId is { } topicId) query = query.Where(q => q.TopicId == topicId);
        if (request.Type is { } type) query = query.Where(q => q.Type == type);
        if (request.Difficulty is { } difficulty) query = query.Where(q => q.Difficulty == difficulty);
        if (request.BloomLevel is { } bloom) query = query.Where(q => q.BloomLevel == bloom);
        if (request.Source is { } source) query = query.Where(q => q.Source == source);

        query = request.Status is { } status
            ? query.Where(q => q.Status == status)
            : query.Where(q => q.Status != QuestionStatus.Archived);

        if (request.ExcludeExamId is { } examId)
            query = query.Where(q => !q.ExamQuestions.Any(eq => eq.ExamId == examId));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(q =>
                q.Text.Contains(term) || q.QuestionTags.Any(qt => qt.Tag.Name.Contains(term)));
        }

        return await query
            .OrderByDescending(q => q.UpdatedAt ?? q.CreatedAt)
            .ThenBy(q => q.Id)
            .Select(q => new QuestionListItemDto(
                q.Id,
                q.CourseId,
                q.Course.Code,
                q.TopicId,
                q.Topic != null ? q.Topic.Name : null,
                q.Text,
                q.Type,
                q.Difficulty,
                q.BloomLevel,
                q.Points,
                q.Status,
                q.Source,
                q.Options.Count,
                q.ExamQuestions.Count,
                q.QuestionTags.Select(qt => qt.Tag.Name).OrderBy(n => n).ToList(),
                q.CreatedAt,
                q.UpdatedAt))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}

// ---------- GetQuestionById ----------

public sealed record GetQuestionByIdQuery(Guid Id) : IRequest<QuestionDto>;

public sealed class GetQuestionByIdQueryHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<GetQuestionByIdQuery, QuestionDto>
{
    public async Task<QuestionDto> Handle(GetQuestionByIdQuery request, CancellationToken cancellationToken)
    {
        var courseId = await db.Questions
            .Where(q => q.Id == request.Id)
            .Select(q => (Guid?)q.CourseId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Question), request.Id);

        await access.EnsureCourseAccessAsync(courseId, cancellationToken);

        return await db.Questions
            .AsNoTracking()
            .Where(q => q.Id == request.Id)
            .Select(q => new QuestionDto(
                q.Id,
                q.CourseId,
                q.Course.Code,
                q.Course.Name,
                q.TopicId,
                q.Topic != null ? q.Topic.Name : null,
                q.Text,
                q.Type,
                q.Difficulty,
                q.BloomLevel,
                q.Points,
                q.Explanation,
                q.ExpectedAnswer,
                q.Status,
                q.Source,
                q.Options.OrderBy(o => o.Order)
                    .Select(o => new QuestionOptionDto(o.Id, o.Text, o.IsCorrect, o.Order, o.MatchText))
                    .ToList(),
                q.QuestionTags.Select(qt => qt.Tag.Name).OrderBy(n => n).ToList(),
                q.ExamQuestions.Count,
                q.ExamQuestions.Any(eq => eq.Exam.Status == ExamStatus.Published),
                q.CreatedAt,
                q.UpdatedAt))
            .FirstAsync(cancellationToken);
    }
}

// ---------- GetTags ----------

public sealed record GetTagsQuery(string? Search) : IRequest<IReadOnlyList<string>>;

public sealed class GetTagsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetTagsQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Tags.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(t => t.Name.Contains(term));
        }

        return await query.OrderBy(t => t.Name).Select(t => t.Name).Take(25).ToListAsync(cancellationToken);
    }
}
