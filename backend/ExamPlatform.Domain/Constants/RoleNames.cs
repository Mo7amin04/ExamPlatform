namespace ExamPlatform.Domain.Constants;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";

    public static readonly IReadOnlyList<string> All = [Admin, Teacher];

    // Fixed identifiers so roles can be seeded through migrations in every environment.
    public static readonly Guid AdminRoleId = Guid.Parse("7a1c2f7e-3a55-4a8e-9a55-2f0f1d6b0a01");
    public static readonly Guid TeacherRoleId = Guid.Parse("7a1c2f7e-3a55-4a8e-9a55-2f0f1d6b0a02");
}
