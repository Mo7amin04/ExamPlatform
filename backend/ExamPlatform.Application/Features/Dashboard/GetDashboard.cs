using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Application.Features.Exams;
using ExamPlatform.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Dashboard;

public sealed record DashboardCourseDto(Guid Id, string Code, string Name, string DepartmentName, int QuestionCount, int ExamCount);

public sealed record DashboardQuestionDto(
    Guid Id, string CourseCode, string Text, QuestionType Type, Difficulty Difficulty, QuestionStatus Status,
    QuestionSource Source, DateTime CreatedAt);

public sealed record DashboardDto(
    int CourseCount,
    int QuestionCount,
    int DraftExamCount,
    int PublishedExamCount,
    int AIGeneratedPendingReviewCount,
    IReadOnlyList<DashboardCourseDto> Courses,
    IReadOnlyList<ExamSummaryDto> RecentExams,
    IReadOnlyList<DashboardQuestionDto> RecentQuestions);

/// <summary>Aggregated counters and recent activity for the caller's accessible courses.</summary>
public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardQueryHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private const int RecentCount = 5;

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var courseIds = access.AccessibleCourseIds();
        var questions = db.Questions.AsNoTracking().Where(q => courseIds.Contains(q.CourseId) && q.Status != QuestionStatus.Archived);
        var exams = db.Exams.AsNoTracking().Where(e => courseIds.Contains(e.CourseId));

        var courses = await access.AccessibleCourses()
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new DashboardCourseDto(
                c.Id, c.Code, c.Name, c.Department.Name,
                c.Questions.Count(q => q.Status != QuestionStatus.Archived),
                c.Exams.Count(e => e.Status != ExamStatus.Archived)))
            .ToListAsync(cancellationToken);

        var questionCount = await questions.CountAsync(cancellationToken);
        var pendingAi = await questions.CountAsync(q => q.Source == QuestionSource.AI && q.Status == QuestionStatus.Draft, cancellationToken);
        var draftExams = await exams.CountAsync(e => e.Status == ExamStatus.Draft || e.Status == ExamStatus.Ready, cancellationToken);
        var publishedExams = await exams.CountAsync(e => e.Status == ExamStatus.Published, cancellationToken);

        var recentExams = await exams
            .Where(e => e.Status != ExamStatus.Archived)
            .OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt)
            .Take(RecentCount)
            .Select(ExamProjections.Summary)
            .ToListAsync(cancellationToken);

        var recentQuestions = await questions
            .OrderByDescending(q => q.CreatedAt)
            .Take(RecentCount)
            .Select(q => new DashboardQuestionDto(
                q.Id, q.Course.Code, q.Text, q.Type, q.Difficulty, q.Status, q.Source, q.CreatedAt))
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            courses.Count, questionCount, draftExams, publishedExams, pendingAi,
            courses, recentExams, recentQuestions);
    }
}
