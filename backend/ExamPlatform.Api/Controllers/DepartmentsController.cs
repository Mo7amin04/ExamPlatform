using ExamPlatform.Api.Common;
using ExamPlatform.Api.Security;
using ExamPlatform.Application.Features.Departments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamPlatform.Api.Controllers;

[Route("api/departments")]
[Authorize(Policy = Policies.Staff)]
public sealed class DepartmentsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentDto>>>> List(CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetDepartmentsQuery(), cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<ApiResponse<DepartmentDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateDepartmentCommand command, CancellationToken cancellationToken) =>
        CreatedResponse(await Sender.Send(command, cancellationToken), "Department created successfully.");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(Guid id, UpdateDepartmentCommand command, CancellationToken cancellationToken) =>
        Success(await Sender.Send(command with { Id = id }, cancellationToken), "Department updated successfully.");

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteDepartmentCommand(id), cancellationToken);
        return ApiResponse.Ok("Department deleted successfully.");
    }
}
