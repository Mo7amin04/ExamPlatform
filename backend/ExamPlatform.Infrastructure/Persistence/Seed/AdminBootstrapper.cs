using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Constants;
using ExamPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExamPlatform.Infrastructure.Persistence.Seed;

/// <summary>
/// First-run administrator for non-development environments ("Bootstrap" section).
/// Supply the values through server configuration, never through source control.
/// </summary>
public sealed class BootstrapSettings
{
    public const string SectionName = "Bootstrap";

    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminFullName { get; set; } = "System Administrator";
}

/// <summary>Creates the first administrator when the database has no users and bootstrap values are configured.</summary>
public sealed class AdminBootstrapper(
    ApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<BootstrapSettings> options,
    ILogger<AdminBootstrapper> logger)
{
    public async Task EnsureAdminAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.AdminEmail) || string.IsNullOrWhiteSpace(settings.AdminPassword))
            return;

        if (await db.Users.AnyAsync(cancellationToken))
            return;

        var admin = new User
        {
            FullName = settings.AdminFullName.Trim(),
            Email = settings.AdminEmail.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(settings.AdminPassword),
            IsActive = true
        };
        admin.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = RoleNames.AdminRoleId });

        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Bootstrap administrator {Email} created.", admin.Email);
    }
}
