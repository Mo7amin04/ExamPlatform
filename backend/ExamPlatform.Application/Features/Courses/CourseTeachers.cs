using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Constants;
using ExamPlatform.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Courses;

public sealed record AssignTeacherCommand(Guid CourseId, Guid TeacherId) : IRequest;

public sealed class AssignTeacherCommandValidator : AbstractValidator<AssignTeacherCommand>
{
    public AssignTeacherCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}

public sealed class AssignTeacherCommandHandler(IApplicationDbContext db) : IRequestHandler<AssignTeacherCommand>
{
    public async Task Handle(AssignTeacherCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == request.CourseId, cancellationToken))
            throw new NotFoundException(nameof(Course), request.CourseId);

        var isTeacher = await db.Users.AnyAsync(u =>
            u.Id == request.TeacherId && u.IsActive && u.UserRoles.Any(r => r.Role.Name == RoleNames.Teacher),
            cancellationToken);
        if (!isTeacher)
            throw new ValidationException(nameof(request.TeacherId), "The user is not an active teacher.");

        if (await db.CourseTeachers.AnyAsync(t => t.CourseId == request.CourseId && t.TeacherId == request.TeacherId, cancellationToken))
            throw new ConflictException("The teacher is already assigned to this course.");

        db.CourseTeachers.Add(new CourseTeacher { CourseId = request.CourseId, TeacherId = request.TeacherId });
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record RemoveTeacherCommand(Guid CourseId, Guid TeacherId) : IRequest;

public sealed class RemoveTeacherCommandHandler(IApplicationDbContext db) : IRequestHandler<RemoveTeacherCommand>
{
    public async Task Handle(RemoveTeacherCommand request, CancellationToken cancellationToken)
    {
        var assignment = await db.CourseTeachers
            .FirstOrDefaultAsync(t => t.CourseId == request.CourseId && t.TeacherId == request.TeacherId, cancellationToken)
            ?? throw new NotFoundException("The teacher is not assigned to this course.");

        db.CourseTeachers.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
    }
}
