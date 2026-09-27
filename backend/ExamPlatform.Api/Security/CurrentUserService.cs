using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Infrastructure.Identity;

namespace ExamPlatform.Api.Security;

/// <summary>Reads the caller identity exclusively from the validated JWT principal.</summary>
public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(JwtTokenGenerator.UserIdClaimType)?.Value, out var id) ? id : null;

    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;
}
