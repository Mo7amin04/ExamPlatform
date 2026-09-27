using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Infrastructure.AI;

/// <summary>
/// Development-only provider (AI:Provider = "Mock"). Produces clearly labelled sample questions so the
/// generate → review → accept workflow can be exercised without an API key. Never use in production.
/// </summary>
public sealed class MockAIQuestionGenerator : IAIQuestionGenerator
{
    public string ProviderName => AISettings.MockProvider;
    public string? ModelName => "sample-data";

    public Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(QuestionGenerationRequest request, CancellationToken cancellationToken)
    {
        var subject = request.TopicName ?? request.CourseName;
        IReadOnlyList<GeneratedQuestion> questions = Enumerable.Range(1, request.NumberOfQuestions)
            .Select(i => Build(request.QuestionType, request.Difficulty, request.BloomLevel, subject, i))
            .ToList();
        return Task.FromResult(questions);
    }

    public Task<GeneratedQuestion> ImproveAsync(QuestionImprovementRequest request, CancellationToken cancellationToken)
    {
        var q = request.Question;
        var text = q.Text.Trim();
        if (!text.StartsWith("[Improved]", StringComparison.Ordinal))
            text = "[Improved] " + text;

        return Task.FromResult(q with
        {
            Text = text,
            Explanation = string.IsNullOrWhiteSpace(q.Explanation)
                ? "Sample explanation added by the development AI provider."
                : q.Explanation
        });
    }

    private static GeneratedQuestion Build(QuestionType type, Difficulty difficulty, BloomLevel bloom, string subject, int index)
    {
        var baseQuestion = new GeneratedQuestion
        {
            Type = type,
            Difficulty = difficulty,
            BloomLevel = bloom,
            Points = difficulty switch { Difficulty.Easy => 1, Difficulty.Hard => 3, _ => 2 },
            Explanation = $"Sample explanation for question {index} about {subject}."
        };

        return type switch
        {
            QuestionType.MultipleChoice => baseQuestion with
            {
                Text = $"[Sample] Which statement best describes a key idea of {subject}? (#{index})",
                Options =
                [
                    new QuestionOptionInput($"The correct statement about {subject}", true),
                    new QuestionOptionInput("A common misconception", false),
                    new QuestionOptionInput("An unrelated concept", false),
                    new QuestionOptionInput("A partially correct statement", false)
                ]
            },
            QuestionType.MultipleSelect => baseQuestion with
            {
                Text = $"[Sample] Select all statements that apply to {subject}. (#{index})",
                Options =
                [
                    new QuestionOptionInput("First correct statement", true),
                    new QuestionOptionInput("Second correct statement", true),
                    new QuestionOptionInput("Incorrect statement", false),
                    new QuestionOptionInput("Another incorrect statement", false)
                ]
            },
            QuestionType.TrueFalse => baseQuestion with
            {
                Text = $"[Sample] {subject} is a core concept of this course. (#{index})",
                Options = [new QuestionOptionInput("True", true), new QuestionOptionInput("False", false)]
            },
            QuestionType.ShortAnswer => baseQuestion with
            {
                Text = $"[Sample] Briefly define {subject}. (#{index})",
                ExpectedAnswer = $"A concise definition of {subject}."
            },
            QuestionType.Essay => baseQuestion with
            {
                Text = $"[Sample] Critically discuss the importance of {subject}. (#{index})",
                ExpectedAnswer = "Rubric: definition (2), analysis (4), examples (2), conclusion (2)."
            },
            QuestionType.FillBlank => baseQuestion with
            {
                Text = $"[Sample] The main concept studied in this topic is _____. (#{index})",
                ExpectedAnswer = subject
            },
            QuestionType.Matching => baseQuestion with
            {
                Text = $"[Sample] Match each term related to {subject} with its description. (#{index})",
                Options =
                [
                    new QuestionOptionInput("Term A", false, "Description of term A"),
                    new QuestionOptionInput("Term B", false, "Description of term B"),
                    new QuestionOptionInput("Term C", false, "Description of term C")
                ]
            },
            QuestionType.Ordering => baseQuestion with
            {
                Text = $"[Sample] Arrange the steps of {subject} in the correct order. (#{index})",
                Options =
                [
                    new QuestionOptionInput("First step", false),
                    new QuestionOptionInput("Second step", false),
                    new QuestionOptionInput("Third step", false),
                    new QuestionOptionInput("Fourth step", false)
                ]
            },
            _ => baseQuestion with { Text = $"[Sample] Question {index} about {subject}." }
        };
    }
}
