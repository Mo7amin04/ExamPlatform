using ExamPlatform.Domain.Common;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities;

public class Question : AuditableEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid? TopicId { get; set; }
    public Topic? Topic { get; set; }

    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public Difficulty Difficulty { get; set; }
    public BloomLevel BloomLevel { get; set; }
    public decimal Points { get; set; }
    public string? Explanation { get; set; }

    /// <summary>Expected answer for ShortAnswer/FillBlank, or the marking rubric for Essay.</summary>
    public string? ExpectedAnswer { get; set; }

    public QuestionStatus Status { get; set; } = QuestionStatus.Draft;
    public QuestionSource Source { get; set; } = QuestionSource.Manual;

    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
    public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();

    public bool IsArchived => Status == QuestionStatus.Archived;

    public void Archive() => Status = QuestionStatus.Archived;
}

/// <summary>
/// An answer option. Semantics depend on the question type:
/// choice types use <see cref="IsCorrect"/>; Matching uses <see cref="MatchText"/> as the right-hand pair;
/// Ordering uses <see cref="Order"/> as the correct sequence.
/// </summary>
public class QuestionOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
    public string? MatchText { get; set; }
}

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
}

public class QuestionTag
{
    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
