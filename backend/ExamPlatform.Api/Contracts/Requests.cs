using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Api.Contracts;

// HTTP request bodies for endpoints whose identifiers come from the route.

public sealed record AssignTeacherRequest(Guid TeacherId);

public sealed record SetUserStatusRequest(bool IsActive);

public sealed record ChangeQuestionStatusRequest(QuestionStatus Status);

public sealed record AddExamQuestionsRequest(IReadOnlyList<Guid> QuestionIds, string? Section = null, decimal? Points = null);

public sealed record UpdateExamQuestionRequest(decimal Points, string? Section);

public sealed record ReorderExamQuestionsRequest(IReadOnlyList<Guid> QuestionIds);
