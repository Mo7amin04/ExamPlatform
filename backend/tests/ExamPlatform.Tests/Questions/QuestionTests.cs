using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Enums;
using ExamPlatform.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Tests.Questions;

public class QuestionTests : IDisposable
{
    private readonly TestContext _ctx = new();

    private CreateQuestionCommand ValidMultipleChoice(Guid? courseId = null) => new()
    {
        CourseId = courseId ?? _ctx.Course.Id,
        TopicId = _ctx.Topic.Id,
        Text = "Which keyword starts a loop in Python?",
        Type = QuestionType.MultipleChoice,
        Difficulty = Difficulty.Easy,
        BloomLevel = BloomLevel.Remember,
        Points = 2,
        Options =
        [
            new QuestionOptionInput("for", true),
            new QuestionOptionInput("def", false),
            new QuestionOptionInput("class", false)
        ],
        Tags = ["Loops", "python", "loops "]
    };

    [Fact]
    public async Task CreateQuestion_PersistsQuestionWithOptionsTagsAndAudit()
    {
        var handler = new CreateQuestionCommandHandler(_ctx.Db, _ctx.Writer, new TestSender(_ctx));

        var dto = await handler.Handle(ValidMultipleChoice(), CancellationToken.None);

        Assert.Equal(QuestionStatus.Draft, dto.Status);
        Assert.Equal(QuestionSource.Manual, dto.Source);
        Assert.Equal(3, dto.Options.Count);
        Assert.Equal([1, 2, 3], dto.Options.Select(o => o.Order));
        Assert.Equal(["loops", "python"], dto.Tags);
        Assert.Equal("Loops", dto.TopicName);

        var entity = await _ctx.Db.Questions.SingleAsync(q => q.Id == dto.Id);
        Assert.Equal(_ctx.Teacher.Id, entity.CreatedBy);
        Assert.Equal(DateTimeKind.Utc, entity.CreatedAt.Kind);
    }

    [Fact]
    public async Task CreateQuestion_InCourseTeacherIsNotAssignedTo_IsForbidden()
    {
        var handler = new CreateQuestionCommandHandler(_ctx.Db, _ctx.Writer, new TestSender(_ctx));
        var command = ValidMultipleChoice(_ctx.OtherCourse.Id) with { TopicId = null };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public void InvalidQuestion_FailsValidation()
    {
        var command = ValidMultipleChoice() with
        {
            Text = "",
            Points = 0,
            Options = [new QuestionOptionInput("only one", false)]
        };

        var result = new CreateQuestionCommandValidator().Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Text");
        Assert.Contains(result.Errors, e => e.PropertyName == "Points");
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("between 2 and"));
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("exactly one correct"));
    }

    [Fact]
    public void DuplicateOptions_FailValidation()
    {
        var command = ValidMultipleChoice() with
        {
            Options = [new QuestionOptionInput("Stack", true), new QuestionOptionInput(" stack ", false)]
        };

        var result = new CreateQuestionCommandValidator().Validate(command);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Options must not contain duplicates.");
    }

    [Theory]
    [InlineData(QuestionType.ShortAnswer, null, false)]
    [InlineData(QuestionType.ShortAnswer, "42", true)]
    [InlineData(QuestionType.Essay, null, true)]
    [InlineData(QuestionType.FillBlank, "loop", true)]
    public void TypeSpecificRules_AreEnforced(QuestionType type, string? expectedAnswer, bool valid)
    {
        var command = ValidMultipleChoice() with { Type = type, ExpectedAnswer = expectedAnswer, Options = [] };

        Assert.Equal(valid, new CreateQuestionCommandValidator().Validate(command).IsValid);
    }

    [Fact]
    public async Task SearchAndFilters_ReturnOnlyMatchingAccessibleQuestions()
    {
        _ctx.AddQuestion(text: "Explain recursion", type: QuestionType.Essay, difficulty: Difficulty.Hard);
        _ctx.AddQuestion(text: "What is a for loop?", difficulty: Difficulty.Easy);
        _ctx.AddQuestion(text: "Archived loop question", status: QuestionStatus.Archived);
        _ctx.AddQuestion(course: _ctx.OtherCourse, text: "Loop question in another course");

        var handler = new GetQuestionsQueryHandler(_ctx.Db, _ctx.Access);

        var byText = await handler.Handle(new GetQuestionsQuery { Search = "loop" }, CancellationToken.None);
        Assert.Equal(["What is a for loop?"], byText.Items.Select(q => q.Text));

        var byType = await handler.Handle(new GetQuestionsQuery { Type = QuestionType.Essay }, CancellationToken.None);
        Assert.Single(byType.Items);

        var byDifficulty = await handler.Handle(new GetQuestionsQuery { Difficulty = Difficulty.Hard }, CancellationToken.None);
        Assert.Equal("Explain recursion", Assert.Single(byDifficulty.Items).Text);

        var archived = await handler.Handle(new GetQuestionsQuery { Status = QuestionStatus.Archived }, CancellationToken.None);
        Assert.Equal("Archived loop question", Assert.Single(archived.Items).Text);
    }

    [Fact]
    public async Task Pagination_ReturnsRequestedPageAndTotals()
    {
        for (var i = 0; i < 25; i++)
            _ctx.AddQuestion(text: $"Question {i}");

        var handler = new GetQuestionsQueryHandler(_ctx.Db, _ctx.Access);
        var page3 = await handler.Handle(new GetQuestionsQuery { Page = 3, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(25, page3.TotalCount);
        Assert.Equal(3, page3.TotalPages);
        Assert.Equal(5, page3.Items.Count);
    }

    [Fact]
    public async Task DeleteQuestion_UsedInExam_IsArchivedNotDeleted()
    {
        var used = _ctx.AddQuestion();
        var unused = _ctx.AddQuestion(text: "unused");
        var exam = _ctx.AddExam();
        exam.AddQuestion(used);
        await _ctx.Db.SaveChangesAsync();

        var handler = new DeleteQuestionCommandHandler(_ctx.Db, _ctx.Access);

        Assert.True((await handler.Handle(new DeleteQuestionCommand(used.Id), CancellationToken.None)).Archived);
        Assert.False((await handler.Handle(new DeleteQuestionCommand(unused.Id), CancellationToken.None)).Archived);
        Assert.Equal(QuestionStatus.Archived, (await _ctx.Db.Questions.SingleAsync(q => q.Id == used.Id)).Status);
        Assert.False(await _ctx.Db.Questions.AnyAsync(q => q.Id == unused.Id));
    }

    [Fact]
    public async Task DuplicateQuestion_CreatesIndependentDraftCopy()
    {
        var original = _ctx.AddQuestion();
        var handler = new DuplicateQuestionCommandHandler(_ctx.Db, _ctx.Access, new TestSender(_ctx));

        var copy = await handler.Handle(new DuplicateQuestionCommand(original.Id), CancellationToken.None);

        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal(original.Text, copy.Text);
        Assert.Equal(QuestionStatus.Draft, copy.Status);
        Assert.Equal(2, copy.Options.Count);
        Assert.True(copy.Options.Single(o => o.Text == "4").IsCorrect);
    }

    public void Dispose() => _ctx.Dispose();
}
