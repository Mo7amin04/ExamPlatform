using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Departments;

public sealed record CreateDepartmentCommand(string Name, string Code, string? Description) : IRequest<DepartmentDto>, IDepartmentInput;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator() => Include(new DepartmentInputValidator());
}

public sealed class CreateDepartmentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Departments.AnyAsync(d => d.Code == code, cancellationToken))
            throw new ConflictException($"A department with code '{code}' already exists.");

        var department = new Department
        {
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description?.Trim()
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(department.Id, department.Name, department.Code, department.Description, 0, department.CreatedAt);
    }
}
