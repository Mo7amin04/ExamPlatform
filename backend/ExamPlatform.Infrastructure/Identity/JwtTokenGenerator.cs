using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExamPlatform.Infrastructure.Identity;

/// <summary>JWT options bound from the "Jwt" configuration section.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "ExamPlatform";
    public string Audience { get; set; } = "ExamPlatform.Client";

    /// <summary>HMAC-SHA256 signing key (at least 32 bytes). Must come from secrets/environment in production.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 480;

    public SymmetricSecurityKey GetSecurityKey()
    {
        var bytes = Encoding.UTF8.GetBytes(SigningKey);
        if (bytes.Length < MinimumKeyBytes)
            throw new InvalidOperationException(
                $"Jwt:SigningKey must be at least {MinimumKeyBytes} bytes. Configure it via user-secrets or environment variables.");
        return new SymmetricSecurityKey(bytes);
    }
}

public sealed class JwtTokenGenerator(IOptions<JwtSettings> options, TimeProvider timeProvider) : IJwtTokenGenerator
{
    /// <summary>Short claim names are used as-is (inbound claim mapping is disabled in the API).</summary>
    public const string RoleClaimType = "role";
    public const string NameClaimType = JwtRegisteredClaimNames.Name;
    public const string UserIdClaimType = JwtRegisteredClaimNames.Sub;

    private readonly JwtSettings _settings = options.Value;

    public AccessToken Generate(User user, IReadOnlyCollection<string> roles)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(RoleClaimType, role)));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: new SigningCredentials(_settings.GetSecurityKey(), SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
