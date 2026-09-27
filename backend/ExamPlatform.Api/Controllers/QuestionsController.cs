using ExamPlatform.Api.Common;
using ExamPlatform.Api.Contracts;
using ExamPlatform.Api.Security;
using ExamPlatform.Application.Features.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamPlatform.Api.Controllers;

[Route("api/questions")]
[Authorize(Policy = Policies.Staff)]
public sealed class QuestionsController : ApiControllerBase
{
    /// <summary>Paginated, filtered question bank search.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<QuestionListItemDto>>> List([FromQuery] GetQuestionsQuery query, CancellationToken cancellationToken) =>
        Paged(await Sender.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetQuestionByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType<ApiResponse<QuestionDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateQuestionCommand command, CancellationToken cancellationToken) =>
        CreatedResponse(await Sender.Send(command, cancellationToken), "Question created successfully.");

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> Update(Guid id, UpdateQuestionCommand command, CancellationToken cancellationToken) =>
        Success(await Sender.Send(command with { Id = id }, cancellationToken), "Question updated successfully.");

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> ChangeStatus(Guid id, ChangeQuestionStatusRequest request, CancellationToken cancellationToken)
    {
        await Sender.Send(new ChangeQuestionStatusCommand(id, request.Status), cancellationToken);
        return ApiResponse.Ok($"Question marked as {request.Status}.");
    }

    /// <summary>Deletes an unused question, or archives it when it is referenced by any exam.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeleteQuestionResult>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeleteQuestionCommand(id), cancellationToken);
        return Success(result, result.Archived
            ? "The question is used in exams, so it was archived instead of deleted."
            : "Question deleted successfully.");
    }

    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType<ApiResponse<QuestionDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken cancellationToken) =>
        CreatedResponse(await Sender.Send(new DuplicateQuestionCommand(id), cancellationToken), "Question duplicated successfully.");
}

[Route("api/tags")]
[Authorize(Policy = Policies.Staff)]
public sealed class TagsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> List([FromQuery] string? search, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetTagsQuery(search), cancellationToken));
}
