using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Courses;

public sealed record DeleteCourseCommand(Guid Id) : IRequest;

/// <summary>Deletes a course and its topics. Courses holding questions or exams are protected to preserve history.</summary>
public sealed class DeleteCourseCommandHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<DeleteCourseCommand>
{
    public async Task Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        await access.EnsureCourseAccessAsync(request.Id, cancellationToken);

        if (await db.Questions.AnyAsync(q => q.CourseId == request.Id, cancellationToken)
            || await db.Exams.AnyAsync(e => e.CourseId == request.Id, cancellationToken))
        {
            throw new ConflictException("The course has questions or exams and cannot be deleted.");
        }

        var course = await db.Courses
            .Include(c => c.Topics)
            .Include(c => c.CourseTeachers)
            .FirstAsync(c => c.Id == request.Id, cancellationToken);

        var generations = await db.AIGenerations.Where(g => g.CourseId == request.Id).ToListAsync(cancellationToken);
        db.AIGenerations.RemoveRange(generations);
        db.Courses.Remove(course);
        await db.SaveChangesAsync(cancellationToken);
    }
}
