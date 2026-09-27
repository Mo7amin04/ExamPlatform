using System.Text;
using System.Text.Json;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Infrastructure.AI;

/// <summary>Builds provider-neutral prompts that ask for the structured JSON shape parsed by <see cref="AIResponseParser"/>.</summary>
public static class AIPromptBuilder
{
    public const string SystemInstruction =
        "You are an experienced university professor and assessment designer. " +
        "You write clear, unambiguous, academically rigorous exam questions. " +
        "Always answer with JSON only, exactly matching the requested schema. " +
        "Never include answer hints in the question text.";

    public static string BuildGenerationPrompt(QuestionGenerationRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Write {request.NumberOfQuestions} exam question(s) for the university course \"{request.CourseCode} - {request.CourseName}\".");
        if (!string.IsNullOrWhiteSpace(request.CourseDescription))
            sb.AppendLine($"Course description: {request.CourseDescription}");
        if (!string.IsNullOrWhiteSpace(request.TopicName))
            sb.AppendLine($"Topic: {request.TopicName}{(string.IsNullOrWhiteSpace(request.TopicDescription) ? "" : $" — {request.TopicDescription}")}");

        sb.AppendLine($"Question type: {request.QuestionType}.");
        sb.AppendLine($"Difficulty: {request.Difficulty}.");
        sb.AppendLine($"Bloom's taxonomy level: {request.BloomLevel}.");
        sb.AppendLine();
        sb.AppendLine(TypeRules(request.QuestionType));
        sb.AppendLine(CommonRules);

        if (!string.IsNullOrWhiteSpace(request.AdditionalInstructions))
        {
            sb.AppendLine();
            sb.AppendLine("Additional instructions from the teacher:");
            sb.AppendLine(request.AdditionalInstructions);
        }

        if (!string.IsNullOrWhiteSpace(request.SourceMaterial))
        {
            sb.AppendLine();
            sb.AppendLine("Base the questions strictly on the following course material (between the markers):");
            sb.AppendLine("<<<MATERIAL");
            sb.AppendLine(request.SourceMaterial);
            sb.AppendLine("MATERIAL>>>");
        }

        return sb.ToString();
    }

    public static string BuildImprovementPrompt(QuestionImprovementRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Improve the following exam question for the university course \"{request.CourseCode} - {request.CourseName}\"" +
                      (string.IsNullOrWhiteSpace(request.TopicName) ? "." : $", topic \"{request.TopicName}\"."));
        sb.AppendLine("Improve clarity, correctness, academic quality and distractor plausibility. Keep the same question type and intent.");
        sb.AppendLine(TypeRules(request.Question.Type));
        sb.AppendLine(CommonRules);
        if (!string.IsNullOrWhiteSpace(request.Instructions))
        {
            sb.AppendLine("Teacher instructions:");
            sb.AppendLine(request.Instructions);
        }
        sb.AppendLine("Return exactly one question in the \"questions\" array.");
        sb.AppendLine("Original question (JSON):");
        sb.AppendLine(JsonSerializer.Serialize(new
        {
            text = request.Question.Text,
            type = request.Question.Type.ToString(),
            difficulty = request.Question.Difficulty.ToString(),
            bloomLevel = request.Question.BloomLevel.ToString(),
            points = request.Question.Points,
            explanation = request.Question.Explanation,
            expectedAnswer = request.Question.ExpectedAnswer,
            options = request.Question.Options.Select(o => new { text = o.Text, isCorrect = o.IsCorrect, matchText = o.MatchText })
        }));
        return sb.ToString();
    }

    private const string CommonRules =
        "Output format: {\"questions\": [{\"text\", \"type\", \"difficulty\", \"bloomLevel\", \"points\", \"explanation\", \"expectedAnswer\", \"options\": [{\"text\", \"isCorrect\", \"matchText\"}]}]}. " +
        "Use exactly these enum values — type: MultipleChoice, TrueFalse, MultipleSelect, ShortAnswer, Essay, FillBlank, Matching, Ordering; " +
        "difficulty: Easy, Medium, Hard; bloomLevel: Remember, Understand, Apply, Analyze, Evaluate, Create. " +
        "points is a positive number (typically 1-10, higher for harder or longer questions). " +
        "explanation briefly justifies the correct answer. Options must be distinct.";

    private static string TypeRules(QuestionType type) => type switch
    {
        QuestionType.MultipleChoice => "Rules: 4 options, exactly one with isCorrect=true, plausible distractors. expectedAnswer is empty.",
        QuestionType.MultipleSelect => "Rules: 4-6 options, two or more with isCorrect=true. expectedAnswer is empty.",
        QuestionType.TrueFalse => "Rules: exactly two options \"True\" and \"False\", exactly one with isCorrect=true. expectedAnswer is empty.",
        QuestionType.ShortAnswer => "Rules: options must be an empty array. expectedAnswer contains a concise model answer.",
        QuestionType.Essay => "Rules: options must be an empty array. expectedAnswer contains a marking rubric with key points.",
        QuestionType.FillBlank => "Rules: mark each blank in the text with \"_____\". options must be an empty array. expectedAnswer contains the missing word(s).",
        QuestionType.Matching => "Rules: 4-6 options; each option's text is a left-column item and matchText is its correct right-column match. isCorrect is false.",
        QuestionType.Ordering => "Rules: 4-6 options listed in the CORRECT order; the text asks students to arrange them. isCorrect is false.",
        _ => string.Empty
    };
}
