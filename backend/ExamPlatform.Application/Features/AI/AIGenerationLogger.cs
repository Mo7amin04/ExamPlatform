using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Application.Features.AI;

/// <summary>Runs an AI provider call and records the outcome in the AIGenerations audit table.</summary>
public sealed class AIGenerationLogger(IApplicationDbContext db, IAIQuestionGenerator generator)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false
    };

    public async Task<(T Result, AIGeneration Log)> RunAsync<T>(
        AIOperation operation,
        Guid courseId,
        Guid? topicId,
        int requestedCount,
        object requestPayload,
        Func<CancellationToken, Task<T>> call,
        Func<T, int> countResult,
        CancellationToken cancellationToken)
    {
        var log = new AIGeneration
        {
            CourseId = courseId,
            TopicId = topicId,
            Operation = operation,
            Provider = generator.ProviderName,
            Model = generator.ModelName,
            RequestedCount = requestedCount,
            RequestPayload = JsonSerializer.Serialize(requestPayload, JsonOptions)
        };

        var stopwatch = Stopwatch.StartNew();
        T result;
        try
        {
            result = await call(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.Status = AIGenerationStatus.Failed;
            log.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            await SaveAsync(log, stopwatch);

            if (ex is AIProviderException)
                throw;
            throw new AIProviderException("The AI provider returned an unexpected error.", innerException: ex);
        }

        log.Status = AIGenerationStatus.Succeeded;
        log.GeneratedCount = countResult(result);
        log.ResponsePayload = JsonSerializer.Serialize(result, JsonOptions);
        await SaveAsync(log, stopwatch);
        return (result, log);
    }

    private async Task SaveAsync(AIGeneration log, Stopwatch stopwatch)
    {
        log.DurationMs = (int)stopwatch.ElapsedMilliseconds;
        db.AIGenerations.Add(log);
        // Persist the audit record even if the caller has already given up on the request.
        await db.SaveChangesAsync(CancellationToken.None);
    }
}
