using ExamPlatform.Application.Features.Exams.Preview;

namespace ExamPlatform.Application.Common.Interfaces;

/// <summary>Renders printable exam documents as PDF. Implemented in Infrastructure.</summary>
public interface IExamPdfExporter
{
    byte[] ExportExam(ExamPreviewDto exam);
    byte[] ExportAnswerKey(ExamAnswerKeyDto answerKey);
}

/// <summary>Renders printable exam documents as Word (.docx). Implemented in Infrastructure.</summary>
public interface IExamWordExporter
{
    byte[] ExportExam(ExamPreviewDto exam);
    byte[] ExportAnswerKey(ExamAnswerKeyDto answerKey);
}
