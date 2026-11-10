using System.ComponentModel.DataAnnotations;
using ExamPlatform.Api.Common;
using ExamPlatform.Api.Security;
using ExamPlatform.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExamPlatform.Api.Controllers;

public sealed record ForgotPasswordRequest([Required, EmailAddress, MaxLength(256)] string Email);

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

    /// <summary>Changes the caller's password and invalidates their existing tokens.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.ChangePassword)]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ForgotPassword)]
    public IActionResult ForgotPassword(ForgotPasswordRequest request, [FromServices] PasswordRecovery recovery)
    {
        if (!recovery.TryRequest(request.Email))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse.Fail("Password reset is unavailable. Please contact your administrator or try again later."));

        return Accepted();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ResetPassword)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken);
        return NoContent();
    }
}
