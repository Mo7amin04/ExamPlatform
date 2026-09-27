using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Application.Common.Settings;
using MediatR;
using Microsoft.Extensions.Options;

namespace ExamPlatform.Application.Features.Exams.Preview;

// ---------- GetExamPreview ----------

/// <summary>Student-facing exam paper. Never includes correct answers.</summary>
public sealed record GetExamPreviewQuery(Guid ExamId) : IRequest<ExamPreviewDto>;

public sealed class GetExamPreviewQueryHandler(
    IApplicationDbContext db,
    ICourseAccessService access,
    IOptions<InstitutionSettings> institution) : IRequestHandler<GetExamPreviewQuery, ExamPreviewDto>
{
    public async Task<ExamPreviewDto> Handle(GetExamPreviewQuery request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForDocumentAsync(request.ExamId, access, cancellationToken);
        return ExamDocumentBuilder.BuildPreview(exam, institution.Value.UniversityName);
    }
}

// ---------- GetExamAnswerKey ----------

public sealed record GetExamAnswerKeyQuery(Guid ExamId) : IRequest<ExamAnswerKeyDto>;

public sealed class GetExamAnswerKeyQueryHandler(
    IApplicationDbContext db,
    ICourseAccessService access,
    IOptions<InstitutionSettings> institution) : IRequestHandler<GetExamAnswerKeyQuery, ExamAnswerKeyDto>
{
    public async Task<ExamAnswerKeyDto> Handle(GetExamAnswerKeyQuery request, CancellationToken cancellationToken)
    {
        var exam = await db.LoadForDocumentAsync(request.ExamId, access, cancellationToken);
        return ExamDocumentBuilder.BuildAnswerKey(exam, institution.Value.UniversityName);
    }
}
