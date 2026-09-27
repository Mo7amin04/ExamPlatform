using ExamPlatform.Domain.Entities;

namespace ExamPlatform.Application.Common.Interfaces;

/// <summary>The authenticated caller, always derived from the validated token — never from client input.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user, IReadOnlyCollection<string> roles);
}
