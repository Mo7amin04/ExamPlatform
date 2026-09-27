using ExamPlatform.Domain.Common;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities;

/// <summary>
/// Audit log of AI provider calls. Generated questions are never stored in the question bank here;
/// they only become <see cref="Question"/> rows after a teacher accepts them.
/// </summary>
public class AIGeneration : AuditableEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid? TopicId { get; set; }
    public Topic? Topic { get; set; }

    public AIOperation Operation { get; set; }
    public AIGenerationStatus Status { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int RequestedCount { get; set; }
    public int GeneratedCount { get; set; }
    public int AcceptedCount { get; set; }
    public string RequestPayload { get; set; } = string.Empty;
    public string? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
}
