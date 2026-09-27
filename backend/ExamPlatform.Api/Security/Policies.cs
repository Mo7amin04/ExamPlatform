using ExamPlatform.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ExamPlatform.Api.Security;

/// <summary>Named authorization policies. Course/exam ownership is enforced additionally in the Application layer.</summary>
public static class Policies
{
    /// <summary>Administrators only (department, user and teacher-assignment management).</summary>
    public const string AdminOnly = nameof(AdminOnly);

    /// <summary>Teachers only.</summary>
    public const string TeacherOnly = nameof(TeacherOnly);

    /// <summary>Teachers and administrators (course content, question bank, exams, AI).</summary>
    public const string Staff = nameof(Staff);

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, p => p.RequireAuthenticatedUser().RequireRole(RoleNames.Admin));
        options.AddPolicy(TeacherOnly, p => p.RequireAuthenticatedUser().RequireRole(RoleNames.Teacher));
        options.AddPolicy(Staff, p => p.RequireAuthenticatedUser().RequireRole(RoleNames.Teacher, RoleNames.Admin));

        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    }
}
