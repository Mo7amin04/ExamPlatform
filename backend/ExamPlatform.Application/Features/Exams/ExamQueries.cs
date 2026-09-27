using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Models;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Exams;

// ---------- GetExams ----------

public sealed record GetExamsQuery : PagedQuery, IRequest<PagedResult<ExamSummaryDto>>
{
    public Guid? CourseId { get; init; }
    public ExamStatus? Status { get; init; }
    public ExamType? Type { get; init; }
    public string? Search { get; init; }
}

public sealed class GetExamsQueryValidator : AbstractValidator<GetExamsQuery>
{
    public GetExamsQueryValidator()
    {
        this.AddPagingRules();
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
    }
}

public sealed class GetExamsQueryHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<GetExamsQuery, PagedResult<ExamSummaryDto>>
{
    public async Task<PagedResult<ExamSummaryDto>> Handle(GetExamsQuery request, CancellationToken cancellationToken)
    {
        var accessibleCourseIds = access.AccessibleCourseIds();
        var query = db.Exams.AsNoTracking().Where(e => accessibleCourseIds.Contains(e.CourseId));

        if (request.CourseId is { } courseId) query = query.Where(e => e.CourseId == courseId);
        if (request.Type is { } type) query = query.Where(e => e.Type == type);

        query = request.Status is { } status
            ? query.Where(e => e.Status == status)
            : query.Where(e => e.Status != ExamStatus.Archived);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(e => e.Title.Contains(term) || e.Course.Code.Contains(term));
        }

        return await query
            .OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt)
            .ThenBy(e => e.Id)
            .Select(ExamProjections.Summary)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}

// ---------- GetExamById ----------

public sealed record GetExamByIdQuery(Guid Id) : IRequest<ExamDetailDto>;

public sealed class GetExamByIdQueryHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<GetExamByIdQuery, ExamDetailDto>
{
    public async Task<ExamDetailDto> Handle(GetExamByIdQuery request, CancellationToken cancellationToken)
    {
        var courseId = await db.Exams
            .Where(e => e.Id == request.Id)
            .Select(e => (Guid?)e.CourseId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Exam), request.Id);

        await access.EnsureCourseAccessAsync(courseId, cancellationToken);

        return await db.Exams
            .AsNoTracking()
            .Where(e => e.Id == request.Id)
            .Select(e => new ExamDetailDto(
                e.Id,
                e.CourseId,
                e.Course.Code,
                e.Course.Name,
                e.Title,
                e.Description,
                e.Instructions,
                e.Type,
                e.Status,
                e.DurationMinutes,
                e.TotalPoints,
                e.ExamDate,
                e.Versions.Count,
                e.Questions
                    .OrderBy(eq => eq.Order)
                    .Select(eq => new ExamQuestionDto(
                        eq.QuestionId,
                        eq.Order,
                        eq.Points,
                        eq.Section,
                        eq.Question.Text,
                        eq.Question.Type,
                        eq.Question.Difficulty,
                        eq.Question.BloomLevel,
                        eq.Question.Status,
                        eq.Question.Topic != null ? eq.Question.Topic.Name : null,
                        eq.Question.Points,
                        eq.Question.Options
                            .OrderBy(o => o.Order)
                            .Select(o => new QuestionOptionDto(o.Id, o.Text, o.IsCorrect, o.Order, o.MatchText))
                            .ToList()))
                    .ToList(),
                e.CreatedAt,
                e.UpdatedAt))
            .FirstAsync(cancellationToken);
    }
}

internal static class ExamProjections
{
    public static readonly System.Linq.Expressions.Expression<Func<Exam, ExamSummaryDto>> Summary = e =>
        new ExamSummaryDto(
            e.Id,
            e.CourseId,
            e.Course.Code,
            e.Course.Name,
            e.Title,
            e.Type,
            e.Status,
            e.DurationMinutes,
            e.TotalPoints,
            e.ExamDate,
            e.Questions.Count,
            e.CreatedAt,
            e.UpdatedAt);
}
