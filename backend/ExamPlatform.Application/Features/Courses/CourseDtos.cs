using ExamPlatform.Application.Features.Topics;
using FluentValidation;

namespace ExamPlatform.Application.Features.Courses;

public sealed record CourseTeacherDto(Guid Id, string FullName, string Email);

public sealed record CourseDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    string Code,
    string Name,
    string? Description,
    int CreditHours,
    int TopicCount,
    int QuestionCount,
    int ExamCount,
    IReadOnlyList<CourseTeacherDto> Teachers,
    DateTime CreatedAt);

public sealed record CourseDetailDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    string Code,
    string Name,
    string? Description,
    int CreditHours,
    int QuestionCount,
    int ExamCount,
    IReadOnlyList<CourseTeacherDto> Teachers,
    IReadOnlyList<TopicDto> Topics,
    DateTime CreatedAt);

public interface ICourseInput
{
    Guid DepartmentId { get; }
    string Code { get; }
    string Name { get; }
    string? Description { get; }
    int CreditHours { get; }
}

public sealed class CourseInputValidator : AbstractValidator<ICourseInput>
{
    public CourseInputValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9 _-]+$")
            .WithMessage("Code may only contain letters, digits, spaces, '-' and '_'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.CreditHours).InclusiveBetween(0, 12);
    }
}
