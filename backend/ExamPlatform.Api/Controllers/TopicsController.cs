using ExamPlatform.Api.Common;
using ExamPlatform.Api.Security;
using ExamPlatform.Application.Features.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamPlatform.Api.Controllers;

[Route("api/topics")]
[Authorize(Policy = Policies.Staff)]
public sealed class TopicsController : ApiControllerBase
{
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TopicDto>>> Update(Guid id, UpdateTopicCommand command, CancellationToken cancellationToken) =>
        Success(await Sender.Send(command with { Id = id }, cancellationToken), "Topic updated successfully.");

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteTopicCommand(id), cancellationToken);
        return ApiResponse.Ok("Topic deleted successfully.");
    }
}
