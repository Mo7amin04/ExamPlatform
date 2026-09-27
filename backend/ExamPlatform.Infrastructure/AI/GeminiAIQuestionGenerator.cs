using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExamPlatform.Infrastructure.AI;

/// <summary>Google Gemini implementation using the REST generateContent endpoint with structured JSON output.</summary>
public sealed class GeminiAIQuestionGenerator(
    HttpClient httpClient,
    IOptions<AISettings> options,
    ILogger<GeminiAIQuestionGenerator> logger) : IAIQuestionGenerator
{
    private readonly AISettings _settings = options.Value;

    public string ProviderName => AISettings.GeminiProvider;

    public string? ModelName => string.IsNullOrWhiteSpace(_settings.Model) ? AISettings.DefaultGeminiModel : _settings.Model;

    public async Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(QuestionGenerationRequest request, CancellationToken cancellationToken)
    {
        var text = await CallAsync(AIPromptBuilder.BuildGenerationPrompt(request), cancellationToken);
        return AIResponseParser.Parse(text, request.QuestionType);
    }

    public async Task<GeneratedQuestion> ImproveAsync(QuestionImprovementRequest request, CancellationToken cancellationToken)
    {
        var text = await CallAsync(AIPromptBuilder.BuildImprovementPrompt(request), cancellationToken);
        return AIResponseParser.Parse(text, request.Question.Type)[0];
    }

    private async Task<string> CallAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new AIProviderException(
                "The AI provider is not configured. Set AI:ApiKey via user-secrets or the AI__ApiKey environment variable.",
                isConfigurationError: true);
        }

        var body = new JsonObject
        {
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = AIPromptBuilder.SystemInstruction })
            },
            ["contents"] = new JsonArray(new JsonObject
            {
                ["role"] = "user",
                ["parts"] = new JsonArray(new JsonObject { ["text"] = prompt })
            }),
            ["generationConfig"] = new JsonObject
            {
                ["temperature"] = _settings.Temperature,
                ["responseMimeType"] = "application/json",
                ["responseSchema"] = ResponseSchema()
            }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{Uri.EscapeDataString(ModelName!)}:generateContent")
        {
            Content = JsonContent.Create(body)
        };
        message.Headers.Add("x-goog-api-key", _settings.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(message, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AIProviderException("The AI provider did not respond in time. Please try again.", innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Gemini request failed");
            throw new AIProviderException("The AI provider could not be reached. Please try again later.", innerException: ex);
        }

        using (response)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Log the provider details server-side only; never echo them (they may reference the key) to clients.
                logger.LogWarning("Gemini returned {StatusCode}: {Payload}", (int)response.StatusCode,
                    payload.Length > 1000 ? payload[..1000] : payload);

                throw response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        new AIProviderException("The AI provider rejected the configured credentials.", isConfigurationError: true),
                    HttpStatusCode.TooManyRequests =>
                        new AIProviderException("The AI provider rate limit was reached. Please wait and try again."),
                    _ => new AIProviderException($"The AI provider returned an error ({(int)response.StatusCode}).")
                };
            }

            return ExtractText(payload);
        }
    }

    private static string ExtractText(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.TryGetProperty("promptFeedback", out var feedback) &&
                feedback.TryGetProperty("blockReason", out var blockReason))
            {
                throw new AIProviderException($"The AI provider blocked the request ({blockReason.GetString()}).");
            }

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                throw new AIProviderException("The AI provider returned no candidates.");

            var candidate = candidates[0];
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts))
            {
                var reason = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : "unknown";
                throw new AIProviderException($"The AI provider returned no content (finish reason: {reason}).");
            }

            return string.Concat(parts.EnumerateArray()
                .Where(p => p.TryGetProperty("text", out _))
                .Select(p => p.GetProperty("text").GetString()));
        }
        catch (JsonException ex)
        {
            throw new AIProviderException("The AI provider returned an unreadable response.", innerException: ex);
        }
    }

    private static JsonObject ResponseSchema()
    {
        static JsonArray Values<T>() where T : struct, Enum =>
            new(Enum.GetNames<T>().Select(n => (JsonNode)JsonValue.Create(n)!).ToArray());

        return new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["questions"] = new JsonObject
                {
                    ["type"] = "ARRAY",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "OBJECT",
                        ["properties"] = new JsonObject
                        {
                            ["text"] = new JsonObject { ["type"] = "STRING" },
                            ["type"] = new JsonObject { ["type"] = "STRING", ["enum"] = Values<QuestionType>() },
                            ["difficulty"] = new JsonObject { ["type"] = "STRING", ["enum"] = Values<Difficulty>() },
                            ["bloomLevel"] = new JsonObject { ["type"] = "STRING", ["enum"] = Values<BloomLevel>() },
                            ["points"] = new JsonObject { ["type"] = "NUMBER" },
                            ["explanation"] = new JsonObject { ["type"] = "STRING" },
                            ["expectedAnswer"] = new JsonObject { ["type"] = "STRING" },
                            ["options"] = new JsonObject
                            {
                                ["type"] = "ARRAY",
                                ["items"] = new JsonObject
                                {
                                    ["type"] = "OBJECT",
                                    ["properties"] = new JsonObject
                                    {
                                        ["text"] = new JsonObject { ["type"] = "STRING" },
                                        ["isCorrect"] = new JsonObject { ["type"] = "BOOLEAN" },
                                        ["matchText"] = new JsonObject { ["type"] = "STRING" }
                                    },
                                    ["required"] = new JsonArray("text", "isCorrect")
                                }
                            }
                        },
                        ["required"] = new JsonArray("text", "type", "difficulty", "bloomLevel", "points", "options")
                    }
                }
            },
            ["required"] = new JsonArray("questions")
        };
    }
}
