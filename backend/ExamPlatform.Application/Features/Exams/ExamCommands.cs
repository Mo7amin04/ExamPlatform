using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Models;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Exams;

// ---------- CreateExam ----------

public sealed record CreateExamCommand(
    Guid CourseId,
    string Title,
    string? Description,
    string? Instructions,
    ExamType Type,
    int DurationMinutes,
    DateTime? ExamDate) : IRequest<ExamDetailDto>, IExamInput;

public sealed class CreateExamCommandValidator : AbstractValidator<CreateExamCommand>
{
    public CreateExamCommandValidator() => Include(new ExamInputValidator());
}

public sealed class CreateExamCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<CreateExamCommand, ExamDetailDto>
{
    public async Task<ExamDetailDto> Handle(CreateExamCommand request, CancellationToken cancellationToken)
    {
        await access.EnsureCourseAccessAsync(request.CourseId, cancellationToken);

        var exam = new Exam
        {
            CourseId = request.CourseId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Instructions = request.Instructions?.Trim(),
            Type = request.Type,
            DurationMinutes = request.DurationMinutes,
            ExamDate = request.ExamDate.AsUtc()
        };

        db.Exams.Add(exam);
        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetExamByIdQuery(exam.Id), cancellationToken);
    }
}

// ---------- UpdateExam ----------

public sealed record UpdateExamCommand(
    Guid Id,
    Guid CourseId,
    string Title,
    string? Description,
    string? Instructions,
    ExamType Type,
    int DurationMinutes,
    DateTime? ExamDate) : IRequest<ExamDetailDto>, IExamInput;

public sealed class UpdateExamCommandValidator : AbstractValidator<UpdateExamCommand>
{
    public UpdateExamCommandValidator() => Include(new ExamInputValidator());
}

/// <summary>Saves exam settings (draft save). Published exams are rejected by the domain model.</summary>
public sealed class UpdateExamCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<UpdateExamCommand, ExamDetailDto>
{
    public async Task<ExamDetailDto> Handle(UpdateExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForUpdateAsync(request.Id, access, cancellationToken);
        exam.EnsureEditable();

        if (exam.CourseId != request.CourseId)
        {
            if (exam.Questions.Count > 0)
                throw new ConflictException("Remove all questions before moving the exam to another course.");
            await access.EnsureCourseAccessAsync(request.CourseId, cancellationToken);
            exam.CourseId = request.CourseId;
        }

        exam.Title = request.Title.Trim();
        exam.Description = request.Description?.Trim();
        exam.Instructions = request.Instructions?.Trim();
        exam.Type = request.Type;
        exam.DurationMinutes = request.DurationMinutes;
        exam.ExamDate = request.ExamDate.AsUtc();

        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetExamByIdQuery(exam.Id), cancellationToken);
    }
}

// ---------- DeleteExam ----------

public sealed record DeleteExamResult(bool Archived);

/// <summary>Deletes never-published exams; exams with published history are archived instead.</summary>
public sealed record DeleteExamCommand(Guid Id) : IRequest<DeleteExamResult>;

public sealed class DeleteExamCommandHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<DeleteExamCommand, DeleteExamResult>
{
    public async Task<DeleteExamResult> Handle(DeleteExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForUpdateAsync(request.Id, access, cancellationToken);
        var hasHistory = exam.Status == ExamStatus.Published
                         || await db.ExamVersions.AnyAsync(v => v.ExamId == exam.Id, cancellationToken);

        if (hasHistory)
        {
            if (exam.Status != ExamStatus.Archived)
                exam.Archive();
        }
        else
        {
            db.Exams.Remove(exam);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new DeleteExamResult(hasHistory);
    }
}

// ---------- AddExamQuestions ----------

public sealed record AddExamQuestionsCommand(Guid ExamId, IReadOnlyList<Guid> QuestionIds, string? Section = null, decimal? Points = null)
    : IRequest<ExamDetailDto>;

public sealed class AddExamQuestionsCommandValidator : AbstractValidator<AddExamQuestionsCommand>
{
    public AddExamQuestionsCommandValidator()
    {
        RuleFor(x => x.QuestionIds).NotEmpty().WithMessage("Select at least one question.")
            .Must(ids => ids.Count <= 100).WithMessage("At most 100 questions can be added at once.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("The same question cannot be added twice.");
        RuleFor(x => x.Points).GreaterThan(0).WithMessage("Points must be greater than zero.").When(x => x.Points.HasValue);
        RuleFor(x => x.Section).MaximumLength(200);
    }
}

public sealed class AddExamQuestionsCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<AddExamQuestionsCommand, ExamDetailDto>
{
    public async Task<ExamDetailDto> Handle(AddExamQuestionsCommand request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForUpdateAsync(request.ExamId, access, cancellationToken);

        var questions = await db.Questions
            .Where(q => request.QuestionIds.Contains(q.Id))
            .ToListAsync(cancellationToken);

        var missing = request.QuestionIds.Except(questions.Select(q => q.Id)).ToList();
        if (missing.Count != 0)
            throw new NotFoundException(nameof(Question), missing[0]);

        // Preserve the caller's selection order.
        foreach (var id in request.QuestionIds)
            exam.AddQuestion(questions.First(q => q.Id == id), request.Points, request.Section);

        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetExamByIdQuery(exam.Id), cancellationToken);
    }
}

// ---------- RemoveExamQuestion ----------

public sealed record RemoveExamQuestionCommand(Guid ExamId, Guid QuestionId) : IRequest<ExamDetailDto>;

public sealed class RemoveExamQuestionCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<RemoveExamQuestionCommand, ExamDetailDto>
{
    public async Task<ExamDetailDto> Handle(RemoveExamQuestionCommand request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForUpdateAsync(request.ExamId, access, cancellationToken);
        exam.RemoveQuestion(request.QuestionId);
        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetExamByIdQuery(exam.Id), cancellationToken);
    }
}

// ---------- UpdateExamQuestion ----------

public sealed record UpdateExamQuestionCommand(Guid ExamId, Guid QuestionId, decimal Points, string? Section) : IRequest<ExamDetailDto>;

public sealed class UpdateExamQuestionCommandValidator : AbstractValidator<UpdateExamQuestionCommand>
{
    public UpdateExamQuestionCommandValidator()
    {
        RuleFor(x => x.Points).GreaterThan(0).WithMessage("Points must be greater than zero.").LessThanOrEqualTo(1000);
        RuleFor(x => x.Section).MaximumLength(200);
    }
}

public sealed class UpdateExamQuestionCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<UpdateExamQuestionCommand, ExamDetailDto>
{
    public async Task<ExamDetailDto> Handle(UpdateExamQuestionCommand request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForUpdateAsync(request.ExamId, access, cancellationToken);
        exam.UpdateQuestion(request.QuestionId, request.Points, request.Section);
        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetExamByIdQuery(exam.Id), cancellationToken);
    }
}

// ---------- ReorderExamQuestions ----------

public sealed record ReorderExamQuestionsCommand(Guid ExamId, IReadOnlyList<Guid> QuestionIds) : IRequest<ExamDetailDto>;

public sealed class ReorderExamQuestionsCommandValidator : AbstractValidator<ReorderExamQuestionsCommand>
{
    public ReorderExamQuestionsCommandValidator() => RuleFor(x => x.QuestionIds).NotNull();
}

public sealed class ReorderExamQuestionsCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<ReorderExamQuestionsCommand, ExamDetailDto>
{
    public async Task<ExamDetailDto> Handle(ReorderExamQuestionsCommand request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForUpdateAsync(request.ExamId, access, cancellationToken);
        exam.ReorderQuestions(request.QuestionIds);
        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetExamByIdQuery(exam.Id), cancellationToken);
    }
}
