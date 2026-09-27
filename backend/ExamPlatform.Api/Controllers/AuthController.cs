using ExamPlatform.Api.Common;
using ExamPlatform.Api.Security;
using ExamPlatform.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExamPlatform.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    /// <summary>Authenticates with email/password and returns a JWT access token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginCommand command, CancellationToken cancellationToken) =>
        Success(await Sender.Send(command, cancellationToken), "Login successful.");

    /// <summary>Returns the authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Me(CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetCurrentUserQuery(), cancellationToken));
}
