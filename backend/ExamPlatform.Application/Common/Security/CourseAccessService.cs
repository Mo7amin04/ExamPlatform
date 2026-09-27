using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Constants;
using ExamPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Common.Security;

/// <summary>
/// Central place for the "teacher can only manage assigned courses" rule.
/// Admins can access every course; teachers only courses they are assigned to.
/// </summary>
public interface ICourseAccessService
{
    Guid CurrentUserId { get; }
    bool IsAdmin { get; }

    /// <summary>Courses visible to the current user (server-side filter).</summary>
    IQueryable<Course> AccessibleCourses();

    /// <summary>Ids of courses visible to the current user, usable as a sub-query.</summary>
    IQueryable<Guid> AccessibleCourseIds();

    /// <summary>Throws <see cref="NotFoundException"/> or <see cref="ForbiddenAccessException"/>.</summary>
    Task EnsureCourseAccessAsync(Guid courseId, CancellationToken cancellationToken);
}

public sealed class CourseAccessService(IApplicationDbContext db, ICurrentUserService currentUser) : ICourseAccessService
{
    public Guid CurrentUserId => currentUser.UserId ?? throw new UnauthorizedException();

    public bool IsAdmin => currentUser.IsInRole(RoleNames.Admin);

    public IQueryable<Course> AccessibleCourses()
    {
        if (IsAdmin)
            return db.Courses;

        var userId = CurrentUserId;
        return db.Courses.Where(c => c.CourseTeachers.Any(t => t.TeacherId == userId));
    }

    public IQueryable<Guid> AccessibleCourseIds()
    {
        if (IsAdmin)
            return db.Courses.Select(c => c.Id);

        var userId = CurrentUserId;
        return db.CourseTeachers.Where(t => t.TeacherId == userId).Select(t => t.CourseId);
    }

    public async Task EnsureCourseAccessAsync(Guid courseId, CancellationToken cancellationToken)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == courseId, cancellationToken))
            throw new NotFoundException(nameof(Course), courseId);

        if (IsAdmin)
            return;

        var userId = CurrentUserId;
        var assigned = await db.CourseTeachers
            .AnyAsync(t => t.CourseId == courseId && t.TeacherId == userId, cancellationToken);

        if (!assigned)
            throw new ForbiddenAccessException("You are not assigned to this course.");
    }
}
