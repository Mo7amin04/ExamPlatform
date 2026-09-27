using ExamPlatform.Api.Common;
using ExamPlatform.Api.Security;
using ExamPlatform.Application.Features.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamPlatform.Api.Controllers;

[Route("api/dashboard")]
[Authorize(Policy = Policies.Staff)]
public sealed class DashboardController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> Get(CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetDashboardQuery(), cancellationToken));
}
