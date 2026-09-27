using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Questions;

// ---------- CreateQuestion ----------

public sealed record CreateQuestionCommand : QuestionInput, IRequest<QuestionDto>;

public sealed class CreateQuestionCommandValidator : QuestionInputValidator<CreateQuestionCommand>;

public sealed class CreateQuestionCommandHandler(IApplicationDbContext db, QuestionWriter writer, ISender sender)
    : IRequestHandler<CreateQuestionCommand, QuestionDto>
{
    public async Task<QuestionDto> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await writer.CreateAsync(request, QuestionSource.Manual, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetQuestionByIdQuery(question.Id), cancellationToken);
    }
}

// ---------- UpdateQuestion ----------

public sealed record UpdateQuestionCommand : QuestionInput, IRequest<QuestionDto>
{
    public Guid Id { get; init; }
}

public sealed class UpdateQuestionCommandValidator : QuestionInputValidator<UpdateQuestionCommand>;

public sealed class UpdateQuestionCommandHandler(IApplicationDbContext db, QuestionWriter writer, ICourseAccessService access, ISender sender)
    : IRequestHandler<UpdateQuestionCommand, QuestionDto>
{
    public async Task<QuestionDto> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await db.Questions
            .Include(q => q.Options)
            .Include(q => q.QuestionTags)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Question), request.Id);

        await access.EnsureCourseAccessAsync(question.CourseId, cancellationToken);

        var usedInExams = await db.ExamQuestions.AnyAsync(eq => eq.QuestionId == question.Id, cancellationToken);
        if (question.CourseId != request.CourseId && usedInExams)
            throw new ConflictException("The question is used in exams and cannot be moved to another course.");

        // Preserve historical exams: content referenced by a published exam must not change underneath it.
        var usedInPublished = await db.ExamQuestions.AnyAsync(
            eq => eq.QuestionId == question.Id && eq.Exam.Status == ExamStatus.Published, cancellationToken);
        if (usedInPublished)
            throw new ConflictException("The question is used in a published exam. Duplicate it to make changes.");

        await writer.EnsureValidTargetAsync(request.CourseId, request.TopicId, cancellationToken);

        question.CourseId = request.CourseId;
        QuestionWriter.Apply(question, request);
        if (request.Status is { } status)
            question.Status = status;
        await writer.ApplyTagsAsync(question, request.Tags, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetQuestionByIdQuery(question.Id), cancellationToken);
    }
}

// ---------- ChangeQuestionStatus ----------

/// <summary>Approve / return to draft / archive. Allowed even for questions used in published exams.</summary>
public sealed record ChangeQuestionStatusCommand(Guid Id, QuestionStatus Status) : IRequest;

public sealed class ChangeQuestionStatusCommandValidator : AbstractValidator<ChangeQuestionStatusCommand>
{
    public ChangeQuestionStatusCommandValidator() => RuleFor(x => x.Status).IsInEnum();
}

public sealed class ChangeQuestionStatusCommandHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<ChangeQuestionStatusCommand>
{
    public async Task Handle(ChangeQuestionStatusCommand request, CancellationToken cancellationToken)
    {
        var question = await db.Questions.FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Question), request.Id);

        await access.EnsureCourseAccessAsync(question.CourseId, cancellationToken);

        question.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- DeleteQuestion ----------

public sealed record DeleteQuestionResult(bool Archived);

/// <summary>Physically deletes unused questions; questions referenced by any exam are archived instead.</summary>
public sealed record DeleteQuestionCommand(Guid Id) : IRequest<DeleteQuestionResult>;

public sealed class DeleteQuestionCommandHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<DeleteQuestionCommand, DeleteQuestionResult>
{
    public async Task<DeleteQuestionResult> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await db.Questions
            .Include(q => q.Options)
            .Include(q => q.QuestionTags)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Question), request.Id);

        await access.EnsureCourseAccessAsync(question.CourseId, cancellationToken);

        var isReferenced =
            await db.ExamQuestions.AnyAsync(eq => eq.QuestionId == question.Id, cancellationToken) ||
            await db.ExamVersionQuestions.AnyAsync(vq => vq.QuestionId == question.Id, cancellationToken);

        if (isReferenced)
            question.Archive();
        else
            db.Questions.Remove(question);

        await db.SaveChangesAsync(cancellationToken);
        return new DeleteQuestionResult(isReferenced);
    }
}

// ---------- DuplicateQuestion ----------

public sealed record DuplicateQuestionCommand(Guid Id) : IRequest<QuestionDto>;

/// <summary>Creates an independent draft copy (content, options and tags) in the same course.</summary>
public sealed class DuplicateQuestionCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<DuplicateQuestionCommand, QuestionDto>
{
    public async Task<QuestionDto> Handle(DuplicateQuestionCommand request, CancellationToken cancellationToken)
    {
        var source = await db.Questions
            .AsNoTracking()
            .Include(q => q.Options)
            .Include(q => q.QuestionTags)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Question), request.Id);

        await access.EnsureCourseAccessAsync(source.CourseId, cancellationToken);

        var copy = new Question
        {
            CourseId = source.CourseId,
            TopicId = source.TopicId,
            Text = source.Text,
            Type = source.Type,
            Difficulty = source.Difficulty,
            BloomLevel = source.BloomLevel,
            Points = source.Points,
            Explanation = source.Explanation,
            ExpectedAnswer = source.ExpectedAnswer,
            Status = QuestionStatus.Draft,
            Source = source.Source
        };

        foreach (var option in source.Options.OrderBy(o => o.Order))
        {
            copy.Options.Add(new QuestionOption
            {
                QuestionId = copy.Id,
                Text = option.Text,
                IsCorrect = option.IsCorrect,
                MatchText = option.MatchText,
                Order = option.Order
            });
        }

        foreach (var tag in source.QuestionTags)
            copy.QuestionTags.Add(new QuestionTag { QuestionId = copy.Id, TagId = tag.TagId });

        db.Questions.Add(copy);
        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetQuestionByIdQuery(copy.Id), cancellationToken);
    }
}
