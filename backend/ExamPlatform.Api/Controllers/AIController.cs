using ExamPlatform.Api.Common;
using ExamPlatform.Api.Security;
using ExamPlatform.Application.Features.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExamPlatform.Api.Controllers;

/// <summary>
/// AI-assisted authoring. Generation and improvement only return proposals; questions enter the
/// bank exclusively through <see cref="Accept"/> after teacher review.
/// </summary>
[Route("api/ai")]
[Authorize(Policy = Policies.Staff)]
[EnableRateLimiting(RateLimitPolicies.AI)]
public sealed class AIController : ApiControllerBase
{
    [HttpPost("questions/generate")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<GenerateQuestionsResult>>> Generate(GenerateQuestionsCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Success(result, $"{result.Questions.Count} question(s) generated. Review them before accepting.");
    }

    [HttpPost("questions/improve")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<GeneratedQuestionDto>>> Improve(ImproveQuestionCommand command, CancellationToken cancellationToken) =>
        Success(await Sender.Send(command, cancellationToken), "Improved version generated. Review it before applying.");

    /// <summary>Saves teacher-approved generated questions to the question bank as drafts.</summary>
    [HttpPost("questions/accept")]
    [ProducesResponseType<ApiResponse<AcceptGeneratedQuestionsResult>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Accept(AcceptGeneratedQuestionsCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return CreatedResponse(result, $"{result.QuestionIds.Count} question(s) added to the question bank as drafts.");
    }

    /// <summary>Extracts text from uploaded course material (.pdf, .docx, .pptx) to ground AI generation. Files are not stored.</summary>
    [HttpPost("materials/extract")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<MaterialTextDto>>> ExtractMaterial(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await Sender.Send(new ExtractMaterialTextCommand(file.FileName, file.Length, stream), cancellationToken);
        return Success(result, result.Truncated ? "Material extracted (truncated to the maximum supported length)." : "Material extracted.");
    }
}
