using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Courses;

public sealed record UpdateCourseCommand(Guid Id, Guid DepartmentId, string Code, string Name, string? Description, int CreditHours)
    : IRequest<CourseDetailDto>, ICourseInput;

public sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator() => Include(new CourseInputValidator());
}

public sealed class UpdateCourseCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<UpdateCourseCommand, CourseDetailDto>
{
    public async Task<CourseDetailDto> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        await access.EnsureCourseAccessAsync(request.Id, cancellationToken);

        if (!await db.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken))
            throw new ValidationException(nameof(request.DepartmentId), "The selected department does not exist.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Courses.AnyAsync(c => c.Code == code && c.Id != request.Id, cancellationToken))
            throw new ConflictException($"A course with code '{code}' already exists.");

        var course = await db.Courses.FirstAsync(c => c.Id == request.Id, cancellationToken);
        course.DepartmentId = request.DepartmentId;
        course.Code = code;
        course.Name = request.Name.Trim();
        course.Description = request.Description?.Trim();
        course.CreditHours = request.CreditHours;

        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetCourseByIdQuery(course.Id), cancellationToken);
    }
}
