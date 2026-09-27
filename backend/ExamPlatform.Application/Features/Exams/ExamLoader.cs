using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Exams;

internal static class ExamLoader
{
    /// <summary>Loads a tracked exam with its questions for mutation, enforcing course access.</summary>
    public static async Task<Exam> LoadForUpdateAsync(
        this IApplicationDbContext db, Guid examId, ICourseAccessService access, CancellationToken cancellationToken)
    {
        var exam = await db.Exams
            .Include(e => e.Questions).ThenInclude(eq => eq.Question)
            .FirstOrDefaultAsync(e => e.Id == examId, cancellationToken)
            ?? throw new NotFoundException(nameof(Exam), examId);

        await access.EnsureCourseAccessAsync(exam.CourseId, cancellationToken);
        return exam;
    }

    /// <summary>Loads a read-only exam graph (course, department, questions, options) for previews/exports.</summary>
    public static async Task<Exam> LoadForDocumentAsync(
        this IApplicationDbContext db, Guid examId, ICourseAccessService access, CancellationToken cancellationToken)
    {
        var exam = await db.Exams
            .AsNoTracking()
            .Include(e => e.Course).ThenInclude(c => c.Department)
            .Include(e => e.Questions).ThenInclude(eq => eq.Question).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == examId, cancellationToken)
            ?? throw new NotFoundException(nameof(Exam), examId);

        await access.EnsureCourseAccessAsync(exam.CourseId, cancellationToken);
        return exam;
    }
}
