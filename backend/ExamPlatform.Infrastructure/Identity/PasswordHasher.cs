using ExamPlatform.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using IPasswordHasher = ExamPlatform.Application.Common.Interfaces.IPasswordHasher;

namespace ExamPlatform.Infrastructure.Identity;

/// <summary>
/// PBKDF2 (HMAC-SHA512, 100k iterations, random salt) via ASP.NET Core Identity's hasher.
/// Plain-text passwords are never stored.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly User HashSubject = new();
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(HashSubject, password);

    public bool Verify(string passwordHash, string password)
    {
        if (string.IsNullOrEmpty(passwordHash))
            return false;

        try
        {
            return _inner.VerifyHashedPassword(HashSubject, passwordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
