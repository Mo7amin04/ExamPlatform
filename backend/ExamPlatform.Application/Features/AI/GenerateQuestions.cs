using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.AI;

public sealed record GenerateQuestionsResult(
    Guid GenerationId,
    string Provider,
    string? Model,
    IReadOnlyList<GeneratedQuestionDto> Questions);

/// <summary>
/// Asks the configured AI provider for question proposals. Nothing is written to the question bank:
/// results are returned for teacher review and must be accepted explicitly.
/// </summary>
public sealed record GenerateQuestionsCommand : IRequest<GenerateQuestionsResult>
{
    public const int MaxQuestions = 20;
    public const int MaxSourceMaterialLength = 30_000;

    public Guid CourseId { get; init; }
    public Guid? TopicId { get; init; }
    public int NumberOfQuestions { get; init; } = 5;
    public QuestionType QuestionType { get; init; } = QuestionType.MultipleChoice;
    public Difficulty Difficulty { get; init; } = Difficulty.Medium;
    public BloomLevel BloomLevel { get; init; } = BloomLevel.Understand;
    public string? AdditionalInstructions { get; init; }

    /// <summary>Optional course material text (e.g. extracted from an uploaded file) to ground the questions.</summary>
    public string? SourceMaterial { get; init; }
}

public sealed class GenerateQuestionsCommandValidator : AbstractValidator<GenerateQuestionsCommand>
{
    public GenerateQuestionsCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.NumberOfQuestions).InclusiveBetween(1, GenerateQuestionsCommand.MaxQuestions);
        RuleFor(x => x.QuestionType).IsInEnum();
        RuleFor(x => x.Difficulty).IsInEnum();
        RuleFor(x => x.BloomLevel).IsInEnum();
        RuleFor(x => x.AdditionalInstructions).MaximumLength(2000);
        RuleFor(x => x.SourceMaterial).MaximumLength(GenerateQuestionsCommand.MaxSourceMaterialLength);
    }
}

public sealed class GenerateQuestionsCommandHandler(
    IApplicationDbContext db,
    QuestionWriter questionWriter,
    AIGenerationLogger aiLogger,
    IAIQuestionGenerator generator) : IRequestHandler<GenerateQuestionsCommand, GenerateQuestionsResult>
{
    public async Task<GenerateQuestionsResult> Handle(GenerateQuestionsCommand request, CancellationToken cancellationToken)
    {
        await questionWriter.EnsureValidTargetAsync(request.CourseId, request.TopicId, cancellationToken);

        var course = await db.Courses.AsNoTracking().FirstAsync(c => c.Id == request.CourseId, cancellationToken);
        var topic = request.TopicId is { } topicId
            ? await db.Topics.AsNoTracking().FirstAsync(t => t.Id == topicId, cancellationToken)
            : null;

        var providerRequest = new QuestionGenerationRequest(
            course.Code,
            course.Name,
            course.Description,
            topic?.Name,
            topic?.Description,
            request.NumberOfQuestions,
            request.QuestionType,
            request.Difficulty,
            request.BloomLevel,
            request.AdditionalInstructions?.Trim(),
            request.SourceMaterial?.Trim());

        var (questions, log) = await aiLogger.RunAsync(
            AIOperation.Generate,
            request.CourseId,
            request.TopicId,
            request.NumberOfQuestions,
            providerRequest with { SourceMaterial = TruncateForLog(providerRequest.SourceMaterial) },
            ct => generator.GenerateAsync(providerRequest, ct),
            result => result.Count,
            cancellationToken);

        var inspected = questions
            .Take(request.NumberOfQuestions)
            .Select(q => GeneratedQuestionInspector.Inspect(q, request.QuestionType))
            .ToList();

        return new GenerateQuestionsResult(log.Id, generator.ProviderName, generator.ModelName, inspected);
    }

    private static string? TruncateForLog(string? material) =>
        material is { Length: > 500 } ? material[..500] + "…" : material;
}
