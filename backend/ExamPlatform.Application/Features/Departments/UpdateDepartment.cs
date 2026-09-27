using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Departments;

public sealed record UpdateDepartmentCommand(Guid Id, string Name, string Code, string? Description) : IRequest<DepartmentDto>, IDepartmentInput;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator() => Include(new DepartmentInputValidator());
}

public sealed class UpdateDepartmentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Departments.AnyAsync(d => d.Code == code && d.Id != request.Id, cancellationToken))
            throw new ConflictException($"A department with code '{code}' already exists.");

        department.Name = request.Name.Trim();
        department.Code = code;
        department.Description = request.Description?.Trim();
        await db.SaveChangesAsync(cancellationToken);

        var courseCount = await db.Courses.CountAsync(c => c.DepartmentId == department.Id, cancellationToken);
        return new DepartmentDto(department.Id, department.Name, department.Code, department.Description, courseCount, department.CreatedAt);
    }
}
