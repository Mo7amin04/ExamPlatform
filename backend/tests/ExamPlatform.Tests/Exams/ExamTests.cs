using ExamPlatform.Application.Common.Settings;
using ExamPlatform.Application.Features.Exams;
using ExamPlatform.Application.Features.Exams.Preview;
using ExamPlatform.Domain.Enums;
using ExamPlatform.Domain.Exceptions;
using ExamPlatform.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExamPlatform.Tests.Exams;

public class ExamTests : IDisposable
{
    private readonly TestContext _ctx = new();

    [Fact]
    public async Task CreateExam_PersistsDraftWithZeroPoints()
    {
        var handler = new CreateExamCommandHandler(_ctx.Db, _ctx.Access, new TestSender(_ctx));

        var dto = await handler.Handle(
            new CreateExamCommand(_ctx.Course.Id, "Quiz 1", null, null, ExamType.Quiz, 30, new DateTime(2026, 10, 1, 9, 0, 0)),
            CancellationToken.None);

        Assert.Equal(ExamStatus.Draft, dto.Status);
        Assert.Equal(0, dto.TotalPoints);
        Assert.Equal(DateTimeKind.Utc, dto.ExamDate!.Value.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateExam_WithNonPositiveDuration_FailsValidation(int duration)
    {
        var result = new CreateExamCommandValidator().Validate(
            new CreateExamCommand(_ctx.Course.Id, "Quiz", null, null, ExamType.Quiz, duration, null));

        Assert.Contains(result.Errors, e => e.PropertyName == "DurationMinutes");
    }

    [Fact]
    public async Task AddQuestion_AddsInOrderAndCalculatesTotalPoints()
    {
        var q1 = _ctx.AddQuestion(points: 2);
        var q2 = _ctx.AddQuestion(text: "second", points: 3);
        var exam = _ctx.AddExam();
        var handler = new AddExamQuestionsCommandHandler(_ctx.Db, _ctx.Access, new TestSender(_ctx));

        var dto = await handler.Handle(new AddExamQuestionsCommand(exam.Id, [q2.Id, q1.Id]), CancellationToken.None);

        Assert.Equal(5, dto.TotalPoints);
        Assert.Equal([q2.Id, q1.Id], dto.Questions.OrderBy(q => q.Order).Select(q => q.QuestionId));
        Assert.Equal(2, await _ctx.Db.ExamQuestions.CountAsync(eq => eq.ExamId == exam.Id));
    }

    [Fact]
    public async Task AddQuestion_Twice_IsRejected()
    {
        var question = _ctx.AddQuestion();
        var exam = _ctx.AddExam();
        var handler = new AddExamQuestionsCommandHandler(_ctx.Db, _ctx.Access, new TestSender(_ctx));
        await handler.Handle(new AddExamQuestionsCommand(exam.Id, [question.Id]), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AddExamQuestionsCommand(exam.Id, [question.Id]), CancellationToken.None));
        Assert.Contains("already part of the exam", ex.Message);
    }

    [Fact]
    public void AddQuestion_DuplicateIdsInOneRequest_FailValidation()
    {
        var id = Guid.NewGuid();
        var result = new AddExamQuestionsCommandValidator().Validate(new AddExamQuestionsCommand(Guid.NewGuid(), [id, id]));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AddQuestion_FromAnotherCourse_IsRejected()
    {
        var foreign = _ctx.AddQuestion(course: _ctx.OtherCourse);
        var exam = _ctx.AddExam();
        var handler = new AddExamQuestionsCommandHandler(_ctx.Db, _ctx.Access, new TestSender(_ctx));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AddExamQuestionsCommand(exam.Id, [foreign.Id]), CancellationToken.None));
        Assert.Contains("different course", ex.Message);
    }

    [Fact]
    public void TotalPoints_AreRecalculatedFromExamQuestions()
    {
        var exam = _ctx.AddExam();
        var a = _ctx.AddQuestion(points: 2);
        var b = _ctx.AddQuestion(text: "b", points: 4);

        exam.AddQuestion(a);
        exam.AddQuestion(b, points: 6);
        Assert.Equal(8, exam.TotalPoints);

        exam.UpdateQuestion(a.Id, 1.5m, "Part A");
        Assert.Equal(7.5m, exam.TotalPoints);

        exam.RemoveQuestion(b.Id);
        Assert.Equal(1.5m, exam.TotalPoints);
        Assert.Equal(1, exam.Questions.Single().Order);

        Assert.Throws<DomainException>(() => exam.UpdateQuestion(a.Id, 0, null));
    }

    [Fact]
    public void Reorder_RequiresEveryQuestionExactlyOnce()
    {
        var exam = _ctx.AddExam();
        var a = _ctx.AddQuestion();
        var b = _ctx.AddQuestion(text: "b");
        exam.AddQuestion(a);
        exam.AddQuestion(b);

        exam.ReorderQuestions([b.Id, a.Id]);
        Assert.Equal(1, exam.Questions.Single(q => q.QuestionId == b.Id).Order);

        Assert.Throws<DomainException>(() => exam.ReorderQuestions([a.Id]));
        Assert.Throws<DomainException>(() => exam.ReorderQuestions([a.Id, a.Id]));
    }

    [Fact]
    public async Task PublishExam_CreatesVersionSnapshotAndLocksExam()
    {
        var question = _ctx.AddQuestion(points: 4);
        var exam = _ctx.AddExam();
        exam.AddQuestion(question);
        await _ctx.Db.SaveChangesAsync();

        var handler = new PublishExamCommandHandler(_ctx.Db, _ctx.Access, new TestSender(_ctx));
        var dto = await handler.Handle(new PublishExamCommand(exam.Id), CancellationToken.None);

        Assert.Equal(ExamStatus.Published, dto.Status);
        Assert.Equal(1, dto.VersionCount);
        var version = await _ctx.Db.ExamVersions.Include(v => v.Questions).SingleAsync(v => v.ExamId == exam.Id);
        Assert.Equal(4, version.TotalPoints);
        Assert.Equal(question.Id, Assert.Single(version.Questions).QuestionId);

        // Published exams cannot be modified until explicitly moved back to draft.
        var another = _ctx.AddQuestion(text: "late addition");
        var tracked = await _ctx.Db.Exams.Include(e => e.Questions).SingleAsync(e => e.Id == exam.Id);
        Assert.Throws<DomainException>(() => tracked.AddQuestion(another));

        tracked.MoveToDraft();
        tracked.AddQuestion(another);
        Assert.Equal(ExamStatus.Draft, tracked.Status);
    }

    [Fact]
    public void PublishExam_WithoutQuestions_IsRejected()
    {
        var exam = _ctx.AddExam();
        Assert.Throws<DomainException>(() => exam.Publish(1));
    }

    [Fact]
    public async Task Preview_HidesAnswers_AnswerKeyShowsThem()
    {
        var question = _ctx.AddQuestion();
        var exam = _ctx.AddExam();
        exam.AddQuestion(question);
        await _ctx.Db.SaveChangesAsync();
        var institution = Options.Create(new InstitutionSettings { UniversityName = "Test University" });

        var preview = await new GetExamPreviewQueryHandler(_ctx.Db, _ctx.Access, institution)
            .Handle(new GetExamPreviewQuery(exam.Id), CancellationToken.None);
        var key = await new GetExamAnswerKeyQueryHandler(_ctx.Db, _ctx.Access, institution)
            .Handle(new GetExamAnswerKeyQuery(exam.Id), CancellationToken.None);

        Assert.Equal("Test University", preview.Header.UniversityName);
        Assert.Equal(["A", "B"], preview.Questions.Single().Options.Select(o => o.Label));
        Assert.Equal("B. 4", key.Items.Single().CorrectAnswer);
    }

    public void Dispose() => _ctx.Dispose();
}
