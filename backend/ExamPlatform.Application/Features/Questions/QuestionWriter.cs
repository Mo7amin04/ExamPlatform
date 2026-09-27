using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Questions;

/// <summary>
/// Shared write logic for questions (manual create/update, duplicate, AI accept) so the mapping rules
/// for options and tags live in one place.
/// </summary>
public sealed class QuestionWriter(IApplicationDbContext db, ICourseAccessService access)
{
    /// <summary>Verifies course access and that the topic (if any) belongs to the course.</summary>
    public async Task EnsureValidTargetAsync(Guid courseId, Guid? topicId, CancellationToken cancellationToken)
    {
        await access.EnsureCourseAccessAsync(courseId, cancellationToken);

        if (topicId is { } id && !await db.Topics.AnyAsync(t => t.Id == id && t.CourseId == courseId, cancellationToken))
            throw new ValidationException("TopicId", "The topic does not belong to the selected course.");
    }

    public async Task<Question> CreateAsync(QuestionInput input, QuestionSource source, CancellationToken cancellationToken)
    {
        await EnsureValidTargetAsync(input.CourseId, input.TopicId, cancellationToken);

        var question = new Question
        {
            CourseId = input.CourseId,
            Source = source,
            // AI-generated content is never trusted automatically: it always starts as a draft.
            Status = source == QuestionSource.AI ? QuestionStatus.Draft : input.Status ?? QuestionStatus.Draft
        };
        Apply(question, input);
        await ApplyTagsAsync(question, input.Tags, cancellationToken);

        db.Questions.Add(question);
        return question;
    }

    /// <summary>Copies classification and content onto the entity, replacing the options.</summary>
    public static void Apply(Question question, QuestionInput input)
    {
        question.TopicId = input.TopicId;
        question.Difficulty = input.Difficulty;
        question.BloomLevel = input.BloomLevel;
        question.Explanation = Normalize(input.Explanation);
        ApplyContent(question, input);
    }

    public static void ApplyContent(Question question, IQuestionContent content)
    {
        question.Text = content.Text.Trim();
        question.Type = content.Type;
        question.Points = content.Points;
        question.ExpectedAnswer = Normalize(content.ExpectedAnswer);

        question.Options.Clear();
        if (!QuestionTypeRules.UsesOptions(content.Type))
            return;

        var order = 1;
        foreach (var option in content.Options)
        {
            question.Options.Add(new QuestionOption
            {
                QuestionId = question.Id,
                Text = option.Text.Trim(),
                IsCorrect = content.Type is QuestionType.MultipleChoice or QuestionType.MultipleSelect or QuestionType.TrueFalse
                            && option.IsCorrect,
                MatchText = content.Type == QuestionType.Matching ? Normalize(option.MatchText) : null,
                Order = order++
            });
        }
    }

    /// <summary>Synchronizes tags by diff (tags are global and created on first use).</summary>
    public async Task ApplyTagsAsync(Question question, IReadOnlyList<string> tagNames, CancellationToken cancellationToken)
    {
        var wanted = tagNames
            .Select(t => string.Join(' ', t.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();

        var existingTags = wanted.Count == 0
            ? []
            : await db.Tags.Where(t => wanted.Contains(t.Name)).ToListAsync(cancellationToken);

        var tags = wanted
            .Select(name => existingTags.FirstOrDefault(t => t.Name == name) ?? CreateTag(name))
            .ToList();

        var wantedIds = tags.Select(t => t.Id).ToHashSet();
        foreach (var link in question.QuestionTags.Where(qt => !wantedIds.Contains(qt.TagId)).ToList())
            question.QuestionTags.Remove(link);

        var currentIds = question.QuestionTags.Select(qt => qt.TagId).ToHashSet();
        foreach (var tag in tags.Where(t => !currentIds.Contains(t.Id)))
            question.QuestionTags.Add(new QuestionTag { QuestionId = question.Id, TagId = tag.Id, Tag = tag });
    }

    private Tag CreateTag(string name)
    {
        var tag = new Tag { Name = name };
        db.Tags.Add(tag);
        return tag;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
