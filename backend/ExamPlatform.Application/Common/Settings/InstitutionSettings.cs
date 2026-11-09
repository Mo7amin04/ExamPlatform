namespace ExamPlatform.Application.Common.Settings;

/// <summary>Institution branding printed on exam previews and exports. Bound from the "Institution" section.</summary>
public sealed class InstitutionSettings
{
    public const string SectionName = "Institution";

    public string? SchoolName { get; set; }
    public string? UniversityName { get; set; }

    public string DisplayName => !string.IsNullOrWhiteSpace(SchoolName) ? SchoolName.Trim()
        : !string.IsNullOrWhiteSpace(UniversityName) ? UniversityName.Trim()
        : "School";
}
