using System.Threading.RateLimiting;
using ExamPlatform.Api.Common;
using ExamPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace ExamPlatform.Api.Security;

public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string ChangePassword = "change-password";
    public const string ForgotPassword = "forgot-password";
    public const string ResetPassword = "reset-password";
    public const string AI = "ai";

    public static void Register(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
            await context.HttpContext.Response.WriteAsJsonAsync(
                ApiResponse.Fail("Too many requests. Please wait a moment and try again."), cancellationToken);

        // Brute-force protection for credentials, partitioned by client IP.
        options.AddPolicy(Login, http => RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

        options.AddPolicy(ChangePassword, http => RateLimitPartition.GetFixedWindowLimiter(
            http.User.FindFirst(JwtTokenGenerator.UserIdClaimType)?.Value ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

        options.AddPolicy(ForgotPassword, http => RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
        options.AddPolicy(ResetPassword, http => RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

        // Cost protection for AI provider calls, partitioned by authenticated user.
        options.AddPolicy(AI, http => RateLimitPartition.GetFixedWindowLimiter(
            http.User.FindFirst(JwtTokenGenerator.UserIdClaimType)?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    }
}
