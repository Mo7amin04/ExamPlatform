using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Enums;
using FluentValidation;

namespace ExamPlatform.Application.Features.Exams;

public sealed record ExamSummaryDto(
    Guid Id,
    Guid CourseId,
    string CourseCode,
    string CourseName,
    string Title,
    ExamType Type,
    ExamStatus Status,
    int DurationMinutes,
    decimal TotalPoints,
    DateTime? ExamDate,
    int QuestionCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ExamQuestionDto(
    Guid QuestionId,
    int Order,
    decimal Points,
    string? Section,
    string Text,
    QuestionType Type,
    Difficulty Difficulty,
    BloomLevel BloomLevel,
    QuestionStatus QuestionStatus,
    string? TopicName,
    decimal DefaultPoints,
    IReadOnlyList<QuestionOptionDto> Options);

public sealed record ExamDetailDto(
    Guid Id,
    Guid CourseId,
    string CourseCode,
    string CourseName,
    string Title,
    string? Description,
    string? Instructions,
    ExamType Type,
    ExamStatus Status,
    int DurationMinutes,
    decimal TotalPoints,
    DateTime? ExamDate,
    int VersionCount,
    IReadOnlyList<ExamQuestionDto> Questions,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public interface IExamInput
{
    Guid CourseId { get; }
    string Title { get; }
    string? Description { get; }
    string? Instructions { get; }
    ExamType Type { get; }
    int DurationMinutes { get; }
    DateTime? ExamDate { get; }
}

public sealed class ExamInputValidator : AbstractValidator<IExamInput>
{
    public ExamInputValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Instructions).MaximumLength(4000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than zero.")
            .LessThanOrEqualTo(24 * 60);
    }
}
