using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Exams;

// ---------- PublishExam ----------

/// <summary>Publishes the exam and records an immutable <c>ExamVersion</c> snapshot.</summary>
public sealed record PublishExamCommand(Guid ExamId) : IRequest<ExamDetailDto>;

public sealed class PublishExamCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<PublishExamCommand, ExamDetailDto>
{
    public async Task<ExamDetailDto> Handle(PublishExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForUpdateAsync(request.ExamId, access, cancellationToken);

        var nextVersion = (await db.ExamVersions
            .Where(v => v.ExamId == exam.Id)
            .MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0) + 1;

        var version = exam.Publish(nextVersion);
        db.ExamVersions.Add(version);

        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetExamByIdQuery(exam.Id), cancellationToken);
    }
}

// ---------- MarkExamReady ----------

public sealed record MarkExamReadyCommand(Guid ExamId) : IRequest<ExamDetailDto>;

public sealed class MarkExamReadyCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<MarkExamReadyCommand, ExamDetailDto>
{
    public async Task<ExamDetailDto> Handle(MarkExamReadyCommand request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForUpdateAsync(request.ExamId, access, cancellationToken);
        exam.MarkReady();
        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetExamByIdQuery(exam.Id), cancellationToken);
    }
}

// ---------- MoveExamToDraft ----------

/// <summary>The explicit, authorized operation that re-opens a published exam for editing.</summary>
public sealed record MoveExamToDraftCommand(Guid ExamId) : IRequest<ExamDetailDto>;

public sealed class MoveExamToDraftCommandHandler(IApplicationDbContext db, ICourseAccessService access, ISender sender)
    : IRequestHandler<MoveExamToDraftCommand, ExamDetailDto>
{
    public async Task<ExamDetailDto> Handle(MoveExamToDraftCommand request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForUpdateAsync(request.ExamId, access, cancellationToken);
        exam.MoveToDraft();
        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetExamByIdQuery(exam.Id), cancellationToken);
    }
}
