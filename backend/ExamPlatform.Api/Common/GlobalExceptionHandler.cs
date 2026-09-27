using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace ExamPlatform.Api.Common;

/// <summary>
/// Maps exceptions to HTTP status codes and the standard <see cref="ApiResponse"/> envelope.
/// Stack traces and internal details are only included in Development.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, response) = exception switch
        {
            ValidationException ex => (StatusCodes.Status400BadRequest, ApiResponse.Fail(ex.Message, ex.Errors)),
            NotFoundException ex => (StatusCodes.Status404NotFound, ApiResponse.Fail(ex.Message)),
            UnauthorizedException ex => (StatusCodes.Status401Unauthorized, ApiResponse.Fail(ex.Message)),
            ForbiddenAccessException ex => (StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message)),
            ConflictException ex => (StatusCodes.Status409Conflict, ApiResponse.Fail(ex.Message)),
            DomainException ex => (StatusCodes.Status409Conflict, ApiResponse.Fail(ex.Message)),
            AIProviderException { IsConfigurationError: true } ex => (StatusCodes.Status503ServiceUnavailable, ApiResponse.Fail(ex.Message)),
            AIProviderException ex => (StatusCodes.Status502BadGateway, ApiResponse.Fail(ex.Message)),
            BadHttpRequestException ex => (ex.StatusCode, ApiResponse.Fail("The request could not be processed.")),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
                (499, ApiResponse.Fail("The request was cancelled.")),
            _ => (StatusCodes.Status500InternalServerError, ApiResponse.Fail(
                environment.IsDevelopment() ? exception.ToString() : "An unexpected error occurred. Please try again later."))
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogInformation("Request failed with {Status}: {Message}", status, exception.Message);

        if (httpContext.Response.HasStarted)
            return false;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
