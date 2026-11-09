namespace ExamPlatform.Infrastructure.Exports;

internal static class ExamFonts
{
    public const string Family = "Tajawal";
    public const string Regular = "Tajawal-Regular.ttf";
    public const string Bold = "Tajawal-Bold.ttf";

    public static Stream Open(string fileName) =>
        typeof(ExamFonts).Assembly.GetManifestResourceStream($"{typeof(ExamFonts).Namespace}.Fonts.{fileName}")
        ?? throw new InvalidOperationException($"Missing embedded exam font: {fileName}");
}
