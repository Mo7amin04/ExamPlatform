using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Application.Common.Interfaces;

/// <summary>Context handed to the AI provider when generating new questions.</summary>
public sealed record QuestionGenerationRequest(
    string CourseCode,
    string CourseName,
    string? CourseDescription,
    string? TopicName,
    string? TopicDescription,
    int NumberOfQuestions,
    QuestionType QuestionType,
    Difficulty Difficulty,
    BloomLevel BloomLevel,
    string? AdditionalInstructions,
    string? SourceMaterial);

/// <summary>Context handed to the AI provider when improving an existing question.</summary>
public sealed record QuestionImprovementRequest(
    string CourseCode,
    string CourseName,
    string? TopicName,
    GeneratedQuestion Question,
    string? Instructions);

/// <summary>
/// A question proposed by an AI provider. It is untrusted until validated and explicitly accepted by a teacher.
/// </summary>
public sealed record GeneratedQuestion : IQuestionContent
{
    public string Text { get; init; } = string.Empty;
    public QuestionType Type { get; init; }
    public Difficulty Difficulty { get; init; } = Difficulty.Medium;
    public BloomLevel BloomLevel { get; init; } = BloomLevel.Understand;
    public decimal Points { get; init; } = 1;
    public string? Explanation { get; init; }
    public string? ExpectedAnswer { get; init; }
    public IReadOnlyList<QuestionOptionInput> Options { get; init; } = [];
}

/// <summary>
/// Provider-agnostic AI question generation. Implementations live in Infrastructure and are selected by configuration.
/// Implementations must throw <see cref="Exceptions.AIProviderException"/> on provider failures.
/// </summary>
public interface IAIQuestionGenerator
{
    string ProviderName { get; }
    string? ModelName { get; }

    Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(QuestionGenerationRequest request, CancellationToken cancellationToken);

    Task<GeneratedQuestion> ImproveAsync(QuestionImprovementRequest request, CancellationToken cancellationToken);
}

/// <summary>Extracts plain text from uploaded course material (.pdf, .docx, .pptx).</summary>
public interface IDocumentTextExtractor
{
    Task<string> ExtractTextAsync(Stream content, string extension, CancellationToken cancellationToken);
}
