using ExamPlatform.Api.Common;
using ExamPlatform.Api.Contracts;
using ExamPlatform.Api.Security;
using ExamPlatform.Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamPlatform.Api.Controllers;

[Route("api/users")]
[Authorize(Policy = Policies.AdminOnly)]
public sealed class UsersController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> List(
        [FromQuery] string? role, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetUsersQuery(role, search), cancellationToken));

    [HttpPost]
    [ProducesResponseType<ApiResponse<UserDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateUserCommand command, CancellationToken cancellationToken) =>
        CreatedResponse(await Sender.Send(command, cancellationToken), "User created successfully.");

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> SetStatus(Guid id, SetUserStatusRequest request, CancellationToken cancellationToken)
    {
        await Sender.Send(new SetUserActiveCommand(id, request.IsActive), cancellationToken);
        return ApiResponse.Ok(request.IsActive ? "User activated." : "User deactivated.");
    }
}
