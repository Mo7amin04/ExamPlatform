using System.Text.RegularExpressions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Features.Exams.Preview;
using MediatR;

namespace ExamPlatform.Application.Features.Exports;

public enum ExportFormat
{
    Pdf,
    Word
}

public enum ExportDocument
{
    Exam,
    AnswerKey
}

public sealed record ExportedFile(byte[] Content, string ContentType, string FileName);

public sealed record ExportExamQuery(Guid ExamId, ExportFormat Format, ExportDocument Document) : IRequest<ExportedFile>;

public sealed partial class ExportExamQueryHandler(
    ISender sender,
    IExamPdfExporter pdfExporter,
    IExamWordExporter wordExporter) : IRequestHandler<ExportExamQuery, ExportedFile>
{
    public const string PdfContentType = "application/pdf";
    public const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public async Task<ExportedFile> Handle(ExportExamQuery request, CancellationToken cancellationToken)
    {
        byte[] content;
        string title;

        if (request.Document == ExportDocument.AnswerKey)
        {
            var key = await sender.Send(new GetExamAnswerKeyQuery(request.ExamId), cancellationToken);
            title = $"{key.Header.CourseCode} {key.Header.Title} answer key";
            content = request.Format == ExportFormat.Pdf ? pdfExporter.ExportAnswerKey(key) : wordExporter.ExportAnswerKey(key);
        }
        else
        {
            var exam = await sender.Send(new GetExamPreviewQuery(request.ExamId), cancellationToken);
            title = $"{exam.Header.CourseCode} {exam.Header.Title}";
            content = request.Format == ExportFormat.Pdf ? pdfExporter.ExportExam(exam) : wordExporter.ExportExam(exam);
        }

        var extension = request.Format == ExportFormat.Pdf ? "pdf" : "docx";
        var contentType = request.Format == ExportFormat.Pdf ? PdfContentType : DocxContentType;
        return new ExportedFile(content, contentType, $"{ToFileName(title)}.{extension}");
    }

    private static string ToFileName(string title)
    {
        var cleaned = UnsafeFileChars().Replace(title, string.Empty).Trim();
        cleaned = Whitespace().Replace(cleaned, "-");
        return cleaned.Length == 0 ? "exam" : cleaned[..Math.Min(cleaned.Length, 100)];
    }

    [GeneratedRegex(@"[^\p{L}\p{N}\s_-]")]
    private static partial Regex UnsafeFileChars();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
