using System.Net;
using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Features.AI;
using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Enums;
using ExamPlatform.Infrastructure.AI;
using ExamPlatform.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ExamPlatform.Tests.AI;

public class AITests : IDisposable
{
    private readonly TestContext _ctx = new();

    private sealed class FakeGenerator(Func<QuestionGenerationRequest, IReadOnlyList<GeneratedQuestion>> generate) : IAIQuestionGenerator
    {
        public string ProviderName => "Fake";
        public string? ModelName => "fake-1";
        public Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(QuestionGenerationRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(generate(request));
        public Task<GeneratedQuestion> ImproveAsync(QuestionImprovementRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(request.Question);
    }

    private GenerateQuestionsCommandHandler Handler(IAIQuestionGenerator generator) =>
        new(_ctx.Db, _ctx.Writer, new AIGenerationLogger(_ctx.Db, generator), generator);

    private static GeneratedQuestion ValidMc(string text = "What is a stack?") => new()
    {
        Text = text,
        Type = QuestionType.MultipleChoice,
        Points = 2,
        Options = [new QuestionOptionInput("LIFO structure", true), new QuestionOptionInput("FIFO structure", false)]
    };

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void GenerationRequest_WithInvalidCount_FailsValidation(int count)
    {
        var result = new GenerateQuestionsCommandValidator().Validate(
            new GenerateQuestionsCommand { CourseId = _ctx.Course.Id, NumberOfQuestions = count });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GenerateQuestionsCommand.NumberOfQuestions));
    }

    [Fact]
    public void GenerationRequest_WithoutCourse_FailsValidation()
    {
        var result = new GenerateQuestionsCommandValidator().Validate(new GenerateQuestionsCommand { NumberOfQuestions = 3 });
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GenerateQuestionsCommand.CourseId));
    }

    [Fact]
    public async Task Generate_ReturnsInspectedProposals_AndNeverWritesToQuestionBank()
    {
        var generator = new FakeGenerator(_ => [ValidMc(), ValidMc("Broken") with { Options = [] }]);

        var result = await Handler(generator).Handle(
            new GenerateQuestionsCommand { CourseId = _ctx.Course.Id, TopicId = _ctx.Topic.Id, NumberOfQuestions = 2 },
            CancellationToken.None);

        Assert.Equal(2, result.Questions.Count);
        Assert.True(result.Questions[0].IsValid);
        Assert.False(result.Questions[1].IsValid);
        Assert.NotEmpty(result.Questions[1].Issues);

        Assert.False(await _ctx.Db.Questions.AnyAsync());
        var log = await _ctx.Db.AIGenerations.SingleAsync();
        Assert.Equal(AIGenerationStatus.Succeeded, log.Status);
        Assert.Equal(2, log.GeneratedCount);
    }

    [Fact]
    public async Task Generate_WhenProviderFails_ThrowsAIProviderException_AndLogsFailure()
    {
        var generator = new FakeGenerator(_ => throw new HttpRequestException("connection refused"));

        await Assert.ThrowsAsync<AIProviderException>(() => Handler(generator).Handle(
            new GenerateQuestionsCommand { CourseId = _ctx.Course.Id, NumberOfQuestions = 1 }, CancellationToken.None));

        var log = await _ctx.Db.AIGenerations.SingleAsync();
        Assert.Equal(AIGenerationStatus.Failed, log.Status);
        Assert.Contains("connection refused", log.ErrorMessage);
    }

    [Fact]
    public void GeneratedQuestionStructure_IsValidatedWithQuestionBankRules()
    {
        var twoCorrect = ValidMc() with
        {
            Options = [new QuestionOptionInput("A", true), new QuestionOptionInput("B", true)]
        };
        var wrongType = ValidMc() with { Type = QuestionType.TrueFalse };
        var noAnswer = new GeneratedQuestion { Text = "Define recursion", Type = QuestionType.ShortAnswer, Points = 1 };

        Assert.Contains(GeneratedQuestionInspector.Inspect(twoCorrect).Issues, i => i.Contains("exactly one correct"));
        Assert.Contains(GeneratedQuestionInspector.Inspect(wrongType, QuestionType.MultipleChoice).Issues, i => i.Contains("Expected a MultipleChoice"));
        Assert.False(GeneratedQuestionInspector.Inspect(noAnswer).IsValid);
        Assert.True(GeneratedQuestionInspector.Inspect(ValidMc()).IsValid);
    }

    [Fact]
    public async Task Accept_CreatesDraftAIQuestions_OnlyOnExplicitRequest()
    {
        var handler = new AcceptGeneratedQuestionsCommandHandler(_ctx.Db, _ctx.Writer, _ctx.Access);
        var accepted = new AcceptedQuestion
        {
            CourseId = _ctx.Course.Id,
            Text = "What is a stack?",
            Type = QuestionType.MultipleChoice,
            Points = 2,
            Status = QuestionStatus.Approved, // ignored: AI questions always start as drafts
            Options = [new QuestionOptionInput("LIFO", true), new QuestionOptionInput("FIFO", false)]
        };

        var result = await handler.Handle(new AcceptGeneratedQuestionsCommand { Questions = [accepted] }, CancellationToken.None);

        var question = await _ctx.Db.Questions.SingleAsync(q => q.Id == result.QuestionIds.Single());
        Assert.Equal(QuestionSource.AI, question.Source);
        Assert.Equal(QuestionStatus.Draft, question.Status);
    }

    [Fact]
    public void ResponseParser_HandlesCodeFencesAndCasing()
    {
        const string raw = """
            ```json
            {"questions":[{"text":"Q?","type":"multiple choice","difficulty":"HARD","bloomLevel":"apply","points":"3",
              "options":[{"text":"a","isCorrect":true},{"text":"b","isCorrect":false}]}]}
            ```
            """;

        var question = Assert.Single(AIResponseParser.Parse(raw, QuestionType.TrueFalse));

        Assert.Equal(QuestionType.MultipleChoice, question.Type);
        Assert.Equal(Difficulty.Hard, question.Difficulty);
        Assert.Equal(BloomLevel.Apply, question.BloomLevel);
        Assert.Equal(3, question.Points);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{\"questions\": []}")]
    [InlineData("")]
    public void ResponseParser_RejectsUnusableOutput(string raw)
    {
        Assert.Throws<AIProviderException>(() => AIResponseParser.Parse(raw, QuestionType.MultipleChoice));
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    private static GeminiAIQuestionGenerator Gemini(HttpStatusCode status, string body, string apiKey = "test-key") =>
        new(new HttpClient(new StubHandler(status, body)) { BaseAddress = new Uri("https://example.test/") },
            Options.Create(new AISettings { ApiKey = apiKey }),
            NullLogger<GeminiAIQuestionGenerator>.Instance);

    private static QuestionGenerationRequest SampleRequest => new(
        "CS101", "Programming", null, null, null, 1, QuestionType.MultipleChoice, Difficulty.Easy, BloomLevel.Remember, null, null);

    [Fact]
    public async Task Gemini_ProviderError_IsWrappedInAIProviderException()
    {
        var ex = await Assert.ThrowsAsync<AIProviderException>(() =>
            Gemini(HttpStatusCode.InternalServerError, "{\"error\":\"boom\"}").GenerateAsync(SampleRequest, CancellationToken.None));
        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task Gemini_WithoutApiKey_ReportsConfigurationError()
    {
        var ex = await Assert.ThrowsAsync<AIProviderException>(() =>
            Gemini(HttpStatusCode.OK, "{}", apiKey: "").GenerateAsync(SampleRequest, CancellationToken.None));
        Assert.True(ex.IsConfigurationError);
    }

    [Fact]
    public async Task Gemini_ParsesStructuredCandidateText()
    {
        const string body = """
            {"candidates":[{"content":{"parts":[{"text":"{\"questions\":[{\"text\":\"2+2?\",\"type\":\"MultipleChoice\",\"difficulty\":\"Easy\",\"bloomLevel\":\"Remember\",\"points\":1,\"options\":[{\"text\":\"4\",\"isCorrect\":true},{\"text\":\"5\",\"isCorrect\":false}]}]}"}]}}]}
            """;

        var questions = await Gemini(HttpStatusCode.OK, body).GenerateAsync(SampleRequest, CancellationToken.None);

        Assert.Equal("2+2?", Assert.Single(questions).Text);
    }

    public void Dispose() => _ctx.Dispose();
}
