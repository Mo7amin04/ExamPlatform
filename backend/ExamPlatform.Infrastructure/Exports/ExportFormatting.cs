using System.Globalization;
using ExamPlatform.Application.Features.Exams.Preview;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Infrastructure.Exports;

/// <summary>Text helpers shared by the PDF and Word exporters so both documents read identically.</summary>
internal static class ExportFormatting
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string Points(decimal points) =>
        points == 1 ? "1 mark" : $"{points.ToString("0.##", Culture)} marks";

    public static string Number(decimal value) => value.ToString("0.##", Culture);

    public static string Date(DateTime? date) =>
        date is { } d ? d.ToString("dddd, d MMMM yyyy", Culture) : "____________";

    public static string Duration(int minutes) =>
        minutes >= 60 && minutes % 60 == 0 ? $"{minutes / 60} hour{(minutes == 60 ? "" : "s")}"
        : minutes > 60 ? $"{minutes / 60} h {minutes % 60} min"
        : $"{minutes} minutes";

    public static string CourseLine(ExamHeaderDto h) => $"{h.CourseCode} — {h.CourseName}";

    public static string ExamTypeLabel(ExamType type) => type switch
    {
        ExamType.Midterm => "Midterm Examination",
        ExamType.Final => "Final Examination",
        ExamType.Quiz => "Quiz",
        ExamType.Assignment => "Assignment",
        ExamType.Practice => "Practice Exam",
        _ => type.ToString()
    };

    public static string TypeLabel(QuestionType type) => type switch
    {
        QuestionType.MultipleChoice => "Multiple choice",
        QuestionType.TrueFalse => "True / False",
        QuestionType.MultipleSelect => "Multiple select",
        QuestionType.ShortAnswer => "Short answer",
        QuestionType.Essay => "Essay",
        QuestionType.FillBlank => "Fill in the blank",
        QuestionType.Matching => "Matching",
        QuestionType.Ordering => "Ordering",
        _ => type.ToString()
    };

    /// <summary>Short instruction printed under the question stem, or null.</summary>
    public static string? Hint(QuestionType type) => type switch
    {
        QuestionType.MultipleChoice => "Choose one answer.",
        QuestionType.TrueFalse => "Circle True or False.",
        QuestionType.MultipleSelect => "Select all that apply.",
        QuestionType.Matching => "Match each item on the left with the correct item on the right.",
        QuestionType.Ordering => "Write the letters in the correct order.",
        QuestionType.FillBlank => "Fill in the blank(s).",
        _ => null
    };
}
