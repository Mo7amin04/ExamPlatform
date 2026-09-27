using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Application.Features.Questions;

public sealed record QuestionOptionDto(Guid Id, string Text, bool IsCorrect, int Order, string? MatchText);

public sealed record QuestionDto(
    Guid Id,
    Guid CourseId,
    string CourseCode,
    string CourseName,
    Guid? TopicId,
    string? TopicName,
    string Text,
    QuestionType Type,
    Difficulty Difficulty,
    BloomLevel BloomLevel,
    decimal Points,
    string? Explanation,
    string? ExpectedAnswer,
    QuestionStatus Status,
    QuestionSource Source,
    IReadOnlyList<QuestionOptionDto> Options,
    IReadOnlyList<string> Tags,
    int UsageCount,
    bool IsUsedInPublishedExam,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record QuestionListItemDto(
    Guid Id,
    Guid CourseId,
    string CourseCode,
    Guid? TopicId,
    string? TopicName,
    string Text,
    QuestionType Type,
    Difficulty Difficulty,
    BloomLevel BloomLevel,
    decimal Points,
    QuestionStatus Status,
    QuestionSource Source,
    int OptionCount,
    int UsageCount,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
