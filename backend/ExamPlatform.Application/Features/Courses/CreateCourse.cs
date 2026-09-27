using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Courses;

public sealed record CreateCourseCommand(Guid DepartmentId, string Code, string Name, string? Description, int CreditHours)
    : IRequest<CourseDetailDto>, ICourseInput;

public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator() => Include(new CourseInputValidator());
}

/// <summary>Creates a course. A teacher who creates a course is automatically assigned to it.</summary>
public sealed class CreateCourseCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<CreateCourseCommand, CourseDetailDto>
{
    public async Task<CourseDetailDto> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken))
            throw new ValidationException(nameof(request.DepartmentId), "The selected department does not exist.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Courses.AnyAsync(c => c.Code == code, cancellationToken))
            throw new ConflictException($"A course with code '{code}' already exists.");

        var course = new Course
        {
            DepartmentId = request.DepartmentId,
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreditHours = request.CreditHours
        };

        if (!access.IsAdmin)
            course.CourseTeachers.Add(new CourseTeacher { TeacherId = access.CurrentUserId });

        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);

        return await sender.Send(new GetCourseByIdQuery(course.Id), cancellationToken);
    }
}
