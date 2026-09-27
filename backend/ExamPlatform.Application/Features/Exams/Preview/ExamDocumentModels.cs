using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Application.Features.Exams.Preview;

/// <summary>Printable exam header. Contains no internal database identifiers.</summary>
public sealed record ExamHeaderDto(
    string UniversityName,
    string DepartmentName,
    string CourseCode,
    string CourseName,
    string Title,
    string? Description,
    string? Instructions,
    ExamType Type,
    ExamStatus Status,
    DateTime? ExamDate,
    int DurationMinutes,
    decimal TotalPoints,
    int QuestionCount);

/// <summary>A labelled option/item as printed on the exam paper (e.g. "A", "b", "1").</summary>
public sealed record PreviewItemDto(string Label, string Text);

/// <summary>
/// A question as printed for students. Never contains correctness information.
/// <see cref="Options"/> holds choices (choice types), the left column (Matching) or the scrambled items (Ordering);
/// <see cref="MatchItems"/> holds the scrambled right column for Matching.
/// </summary>
public sealed record PreviewQuestionDto(
    int Number,
    string? Section,
    string Text,
    QuestionType Type,
    decimal Points,
    IReadOnlyList<PreviewItemDto> Options,
    IReadOnlyList<PreviewItemDto> MatchItems,
    int AnswerLines);

public sealed record ExamPreviewDto(ExamHeaderDto Header, IReadOnlyList<PreviewQuestionDto> Questions);

public sealed record AnswerKeyItemDto(
    int Number,
    string? Section,
    string QuestionText,
    QuestionType Type,
    decimal Points,
    string CorrectAnswer,
    string? Explanation);

public sealed record ExamAnswerKeyDto(ExamHeaderDto Header, IReadOnlyList<AnswerKeyItemDto> Items);
