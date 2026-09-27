namespace ExamPlatform.Application.Common.Settings;

/// <summary>Institution branding printed on exam previews and exports. Bound from the "Institution" section.</summary>
public sealed class InstitutionSettings
{
    public const string SectionName = "Institution";

    public string UniversityName { get; set; } = "University";
}
