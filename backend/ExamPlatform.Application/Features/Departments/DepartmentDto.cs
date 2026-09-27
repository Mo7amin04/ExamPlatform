using FluentValidation;

namespace ExamPlatform.Application.Features.Departments;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    int CourseCount,
    DateTime CreatedAt);

/// <summary>Fields shared by create/update department commands.</summary>
public interface IDepartmentInput
{
    string Name { get; }
    string Code { get; }
    string? Description { get; }
}

public sealed class DepartmentInputValidator : AbstractValidator<IDepartmentInput>
{
    public DepartmentInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("Code may only contain letters, digits, '-' and '_'.");
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
