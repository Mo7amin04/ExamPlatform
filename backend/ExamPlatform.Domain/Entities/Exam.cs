using ExamPlatform.Domain.Common;
using ExamPlatform.Domain.Enums;
using ExamPlatform.Domain.Exceptions;

namespace ExamPlatform.Domain.Entities;

public class Exam : AuditableEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public ExamType Type { get; set; }
    public int DurationMinutes { get; set; }

    /// <summary>Always derived from <see cref="Questions"/>; see <see cref="RecalculateTotalPoints"/>.</summary>
    public decimal TotalPoints { get; private set; }

    public DateTime? ExamDate { get; set; }
    public ExamStatus Status { get; private set; } = ExamStatus.Draft;

    public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
    public ICollection<ExamVersion> Versions { get; set; } = new List<ExamVersion>();

    public bool IsEditable => Status is ExamStatus.Draft or ExamStatus.Ready;

    public void EnsureEditable()
    {
        if (Status == ExamStatus.Published)
            throw new DomainException("Published exams cannot be modified. Move the exam back to draft first.");
        if (Status == ExamStatus.Archived)
            throw new DomainException("Archived exams cannot be modified.");
    }

    public ExamQuestion AddQuestion(Question question, decimal? points = null, string? section = null)
    {
        EnsureEditable();

        if (question.CourseId != CourseId)
            throw new DomainException("The question belongs to a different course than the exam.");
        if (question.IsArchived)
            throw new DomainException("Archived questions cannot be added to an exam.");
        if (Questions.Any(q => q.QuestionId == question.Id))
            throw new DomainException("This question is already part of the exam.");

        var effectivePoints = points ?? question.Points;
        if (effectivePoints <= 0)
            throw new DomainException("Question points must be greater than zero.");

        var examQuestion = new ExamQuestion
        {
            ExamId = Id,
            QuestionId = question.Id,
            Question = question,
            Order = Questions.Count == 0 ? 1 : Questions.Max(q => q.Order) + 1,
            Points = effectivePoints,
            Section = string.IsNullOrWhiteSpace(section) ? null : section.Trim()
        };

        Questions.Add(examQuestion);
        RecalculateTotalPoints();
        return examQuestion;
    }

    public void RemoveQuestion(Guid questionId)
    {
        EnsureEditable();

        var examQuestion = Questions.FirstOrDefault(q => q.QuestionId == questionId)
            ?? throw new DomainException("The question is not part of this exam.");

        Questions.Remove(examQuestion);
        NormalizeOrder();
        RecalculateTotalPoints();
    }

    public void UpdateQuestion(Guid questionId, decimal points, string? section)
    {
        EnsureEditable();

        if (points <= 0)
            throw new DomainException("Question points must be greater than zero.");

        var examQuestion = Questions.FirstOrDefault(q => q.QuestionId == questionId)
            ?? throw new DomainException("The question is not part of this exam.");

        examQuestion.Points = points;
        examQuestion.Section = string.IsNullOrWhiteSpace(section) ? null : section.Trim();
        RecalculateTotalPoints();
    }

    public void ReorderQuestions(IReadOnlyList<Guid> orderedQuestionIds)
    {
        EnsureEditable();

        var current = Questions.Select(q => q.QuestionId).ToHashSet();
        if (orderedQuestionIds.Count != current.Count
            || orderedQuestionIds.Distinct().Count() != orderedQuestionIds.Count
            || !orderedQuestionIds.All(current.Contains))
        {
            throw new DomainException("The new order must contain every exam question exactly once.");
        }

        for (var i = 0; i < orderedQuestionIds.Count; i++)
        {
            Questions.First(q => q.QuestionId == orderedQuestionIds[i]).Order = i + 1;
        }
    }

    public void RecalculateTotalPoints() => TotalPoints = Questions.Sum(q => q.Points);

    public void MarkReady()
    {
        if (Status != ExamStatus.Draft)
            throw new DomainException("Only draft exams can be marked as ready.");
        EnsureHasQuestions();
        Status = ExamStatus.Ready;
    }

    /// <summary>
    /// Publishes the exam and returns an immutable snapshot of its composition.
    /// Requires the question navigations to be loaded.
    /// </summary>
    public ExamVersion Publish(int versionNumber)
    {
        EnsureEditable();
        EnsureHasQuestions();

        if (Questions.Any(q => q.Question is { IsArchived: true }))
            throw new DomainException("The exam contains archived questions. Remove or replace them before publishing.");

        RecalculateTotalPoints();
        Status = ExamStatus.Published;

        var version = new ExamVersion
        {
            ExamId = Id,
            VersionNumber = versionNumber,
            Title = Title,
            DurationMinutes = DurationMinutes,
            TotalPoints = TotalPoints,
            ExamDate = ExamDate,
            Questions = Questions
                .OrderBy(q => q.Order)
                .Select(q => new ExamVersionQuestion
                {
                    QuestionId = q.QuestionId,
                    Order = q.Order,
                    Points = q.Points,
                    Section = q.Section
                })
                .ToList()
        };

        Versions.Add(version);
        return version;
    }

    /// <summary>Explicit, authorized operation that re-opens a ready/published exam for editing.</summary>
    public void MoveToDraft()
    {
        if (Status == ExamStatus.Draft)
            throw new DomainException("The exam is already a draft.");
        Status = ExamStatus.Draft;
    }

    public void Archive()
    {
        if (Status == ExamStatus.Archived)
            throw new DomainException("The exam is already archived.");
        Status = ExamStatus.Archived;
    }

    private void EnsureHasQuestions()
    {
        if (Questions.Count == 0)
            throw new DomainException("The exam must contain at least one question.");
    }

    private void NormalizeOrder()
    {
        var order = 1;
        foreach (var q in Questions.OrderBy(q => q.Order))
            q.Order = order++;
    }
}

public class ExamQuestion : BaseEntity
{
    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public int Order { get; set; }
    public decimal Points { get; set; }
    public string? Section { get; set; }
}

/// <summary>
/// Immutable snapshot of an exam's composition taken at publish time.
/// Questions are referenced (never copied); questions used in exams are archived rather than deleted,
/// so the historical composition stays intact.
/// </summary>
public class ExamVersion : AuditableEntity
{
    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = null!;

    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal TotalPoints { get; set; }
    public DateTime? ExamDate { get; set; }

    public ICollection<ExamVersionQuestion> Questions { get; set; } = new List<ExamVersionQuestion>();
}

public class ExamVersionQuestion : BaseEntity
{
    public Guid ExamVersionId { get; set; }
    public ExamVersion ExamVersion { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public int Order { get; set; }
    public decimal Points { get; set; }
    public string? Section { get; set; }
}
