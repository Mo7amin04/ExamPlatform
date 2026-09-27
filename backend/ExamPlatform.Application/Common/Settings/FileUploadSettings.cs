namespace ExamPlatform.Application.Common.Settings;

/// <summary>Upload restrictions for course material. Bound from the "FileUpload" section.</summary>
public sealed class FileUploadSettings
{
    public const string SectionName = "FileUpload";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = [".pdf", ".docx", ".pptx"];

    public bool IsAllowedExtension(string? extension) =>
        !string.IsNullOrWhiteSpace(extension)
        && AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
}
