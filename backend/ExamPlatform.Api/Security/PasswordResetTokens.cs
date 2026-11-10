using System.Security.Cryptography;
using System.Text.Json;
using ExamPlatform.Application.Features.Auth;
using Microsoft.AspNetCore.DataProtection;

namespace ExamPlatform.Api.Security;

public sealed class PasswordResetTokens(IDataProtectionProvider protection) : IPasswordResetTokens
{
    private readonly ITimeLimitedDataProtector _protector = protection
        .CreateProtector("ExamPlatform.PasswordReset.v1").ToTimeLimitedDataProtector();

    public string Create(Guid userId, string passwordHash) => _protector.Protect(
        JsonSerializer.Serialize(new PasswordResetTicket(userId, PasswordResetTicket.VersionOf(passwordHash))),
        TimeSpan.FromMinutes(30));

    public PasswordResetTicket? Read(string token)
    {
        try
        {
            return JsonSerializer.Deserialize<PasswordResetTicket>(_protector.Unprotect(token));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or JsonException)
        {
            return null;
        }
    }
}
