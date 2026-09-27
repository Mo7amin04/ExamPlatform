using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Topics;

public sealed record TopicDto(Guid Id, Guid CourseId, string Name, string? Description, int Order, int QuestionCount);

public interface ITopicInput
{
    string Name { get; }
    string? Description { get; }
    int? Order { get; }
}

public sealed class TopicInputValidator : AbstractValidator<ITopicInput>
{
    public TopicInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0).When(x => x.Order.HasValue);
    }
}

// ---------- Query ----------

public sealed record GetTopicsQuery(Guid CourseId) : IRequest<IReadOnlyList<TopicDto>>;

public sealed class GetTopicsQueryHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<GetTopicsQuery, IReadOnlyList<TopicDto>>
{
    public async Task<IReadOnlyList<TopicDto>> Handle(GetTopicsQuery request, CancellationToken cancellationToken)
    {
        await access.EnsureCourseAccessAsync(request.CourseId, cancellationToken);

        return await db.Topics
            .AsNoTracking()
            .Where(t => t.CourseId == request.CourseId)
            .OrderBy(t => t.Order).ThenBy(t => t.Name)
            .Select(t => new TopicDto(t.Id, t.CourseId, t.Name, t.Description, t.Order,
                t.Questions.Count(q => q.Status != QuestionStatus.Archived)))
            .ToListAsync(cancellationToken);
    }
}

// ---------- Create ----------

public sealed record CreateTopicCommand(Guid CourseId, string Name, string? Description, int? Order)
    : IRequest<TopicDto>, ITopicInput;

public sealed class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator() => Include(new TopicInputValidator());
}

public sealed class CreateTopicCommandHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<CreateTopicCommand, TopicDto>
{
    public async Task<TopicDto> Handle(CreateTopicCommand request, CancellationToken cancellationToken)
    {
        await access.EnsureCourseAccessAsync(request.CourseId, cancellationToken);

        var name = request.Name.Trim();
        if (await db.Topics.AnyAsync(t => t.CourseId == request.CourseId && t.Name == name, cancellationToken))
            throw new ConflictException($"The course already has a topic named '{name}'.");

        var order = request.Order
            ?? (await db.Topics.Where(t => t.CourseId == request.CourseId).MaxAsync(t => (int?)t.Order, cancellationToken) ?? 0) + 1;

        var topic = new Topic
        {
            CourseId = request.CourseId,
            Name = name,
            Description = request.Description?.Trim(),
            Order = order
        };

        db.Topics.Add(topic);
        await db.SaveChangesAsync(cancellationToken);

        return new TopicDto(topic.Id, topic.CourseId, topic.Name, topic.Description, topic.Order, 0);
    }
}

// ---------- Update ----------

public sealed record UpdateTopicCommand(Guid Id, string Name, string? Description, int? Order)
    : IRequest<TopicDto>, ITopicInput;

public sealed class UpdateTopicCommandValidator : AbstractValidator<UpdateTopicCommand>
{
    public UpdateTopicCommandValidator() => Include(new TopicInputValidator());
}

public sealed class UpdateTopicCommandHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<UpdateTopicCommand, TopicDto>
{
    public async Task<TopicDto> Handle(UpdateTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await db.Topics.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Topic), request.Id);

        await access.EnsureCourseAccessAsync(topic.CourseId, cancellationToken);

        var name = request.Name.Trim();
        if (await db.Topics.AnyAsync(t => t.CourseId == topic.CourseId && t.Name == name && t.Id != topic.Id, cancellationToken))
            throw new ConflictException($"The course already has a topic named '{name}'.");

        topic.Name = name;
        topic.Description = request.Description?.Trim();
        if (request.Order.HasValue)
            topic.Order = request.Order.Value;

        await db.SaveChangesAsync(cancellationToken);

        var questionCount = await db.Questions.CountAsync(
            q => q.TopicId == topic.Id && q.Status != QuestionStatus.Archived, cancellationToken);
        return new TopicDto(topic.Id, topic.CourseId, topic.Name, topic.Description, topic.Order, questionCount);
    }
}

// ---------- Delete ----------

public sealed record DeleteTopicCommand(Guid Id) : IRequest;

/// <summary>Deletes a topic; its questions stay in the bank and simply lose their topic.</summary>
public sealed class DeleteTopicCommandHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<DeleteTopicCommand>
{
    public async Task Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await db.Topics.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Topic), request.Id);

        await access.EnsureCourseAccessAsync(topic.CourseId, cancellationToken);

        var questions = await db.Questions.Where(q => q.TopicId == topic.Id).ToListAsync(cancellationToken);
        questions.ForEach(q => q.TopicId = null);

        var generations = await db.AIGenerations.Where(g => g.TopicId == topic.Id).ToListAsync(cancellationToken);
        generations.ForEach(g => g.TopicId = null);

        db.Topics.Remove(topic);
        await db.SaveChangesAsync(cancellationToken);
    }
}
