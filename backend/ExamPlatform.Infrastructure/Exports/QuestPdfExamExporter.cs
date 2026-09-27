using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Features.Exams.Preview;
using ExamPlatform.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ExamPlatform.Infrastructure.Exports.ExportFormatting;

namespace ExamPlatform.Infrastructure.Exports;

/// <summary>PDF rendering with QuestPDF (Community license — see docs/setup.md).</summary>
public sealed class QuestPdfExamExporter : IExamPdfExporter
{
    private const string Ink = "#1F2937";
    private const string Muted = "#6B7280";
    private const string Rule = "#D1D5DB";

    static QuestPdfExamExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportExam(ExamPreviewDto exam) =>
        Document.Create(container => container.Page(page =>
        {
            ConfigurePage(page);
            page.Header().Element(c => Header(c, exam.Header, isAnswerKey: false));
            page.Content().PaddingTop(10).Column(column =>
            {
                column.Spacing(14);
                column.Item().Element(c => StudentFields(c));

                if (!string.IsNullOrWhiteSpace(exam.Header.Instructions))
                    column.Item().Element(c => Instructions(c, exam.Header.Instructions!));

                string? currentSection = null;
                foreach (var question in exam.Questions)
                {
                    if (question.Section is { } section && section != currentSection)
                    {
                        currentSection = section;
                        column.Item().PaddingTop(4).BorderBottom(1).BorderColor(Rule).PaddingBottom(2)
                            .Text(section).FontSize(12).Bold();
                    }
                    column.Item().PreventPageBreak().Element(c => Question(c, question));
                }
            });
            page.Footer().Element(c => Footer(c, exam.Header));
        })).GeneratePdf();

    public byte[] ExportAnswerKey(ExamAnswerKeyDto answerKey) =>
        Document.Create(container => container.Page(page =>
        {
            ConfigurePage(page);
            page.Header().Element(c => Header(c, answerKey.Header, isAnswerKey: true));
            page.Content().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(95);
                    columns.RelativeColumn();
                    columns.ConstantColumn(55);
                });

                table.Header(header =>
                {
                    static IContainer HeaderCell(IContainer c) =>
                        c.Background("#F3F4F6").BorderBottom(1).BorderColor(Rule).PaddingVertical(5).PaddingHorizontal(4);

                    header.Cell().Element(HeaderCell).Text("Q#").Bold();
                    header.Cell().Element(HeaderCell).Text("Type").Bold();
                    header.Cell().Element(HeaderCell).Text("Correct answer").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Marks").Bold();
                });

                foreach (var item in answerKey.Items)
                {
                    IContainer Cell(IContainer c) => c.BorderBottom(0.5f).BorderColor(Rule).PaddingVertical(5).PaddingHorizontal(4);

                    table.Cell().Element(Cell).Text(item.Number.ToString()).Bold();
                    table.Cell().Element(Cell).Text(TypeLabel(item.Type)).FontColor(Muted);
                    table.Cell().Element(Cell).Column(col =>
                    {
                        col.Item().Text(item.CorrectAnswer);
                        if (!string.IsNullOrWhiteSpace(item.Explanation))
                            col.Item().PaddingTop(2).Text(item.Explanation).FontSize(8.5f).Italic().FontColor(Muted);
                    });
                    table.Cell().Element(Cell).AlignRight().Text(Number(item.Points));
                }

                table.Footer(footer =>
                {
                    footer.Cell().ColumnSpan(3).PaddingTop(6).AlignRight().Text("Total").Bold();
                    footer.Cell().PaddingTop(6).AlignRight().Text(Number(answerKey.Header.TotalPoints)).Bold();
                });
            });
            page.Footer().Element(c => Footer(c, answerKey.Header));
        })).GeneratePdf();

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.DefaultTextStyle(t => t.FontSize(10.5f).FontColor(Ink).LineHeight(1.3f));
    }

    private static void Header(IContainer container, ExamHeaderDto h, bool isAnswerKey)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(h.UniversityName).FontSize(15).Bold();
            column.Item().AlignCenter().Text(h.DepartmentName).FontSize(11).FontColor(Muted);
            column.Item().AlignCenter().Text(CourseLine(h)).FontSize(11);
            column.Item().PaddingTop(6).AlignCenter().Text(h.Title).FontSize(17).Bold();
            column.Item().AlignCenter().Text(isAnswerKey ? "ANSWER KEY — CONFIDENTIAL" : ExamTypeLabel(h.Type))
                .FontSize(10).FontColor(isAnswerKey ? "#B91C1C" : Muted).SemiBold();

            column.Item().PaddingTop(8).BorderTop(1).BorderBottom(1).BorderColor(Rule).PaddingVertical(5).Row(row =>
            {
                row.RelativeItem().Text(t => { t.Span("Date: ").SemiBold(); t.Span(Date(h.ExamDate)); });
                row.RelativeItem().AlignCenter().Text(t => { t.Span("Duration: ").SemiBold(); t.Span(Duration(h.DurationMinutes)); });
                row.RelativeItem().AlignRight().Text(t => { t.Span("Total marks: ").SemiBold(); t.Span(Number(h.TotalPoints)); });
            });
        });
    }

    private static void StudentFields(IContainer container)
    {
        container.Border(1).BorderColor(Rule).Padding(10).Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Student Name: ______________________________________________");
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Student ID: ________________________");
                row.RelativeItem().AlignRight().Text("Signature: ____________________");
            });
        });
    }

    private static void Instructions(IContainer container, string instructions)
    {
        container.Background("#F9FAFB").Padding(8).Column(column =>
        {
            column.Item().Text("Instructions").Bold();
            column.Item().Text(instructions);
        });
    }

    private static void Question(IContainer container, PreviewQuestionDto q)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Row(row =>
            {
                row.ConstantItem(24).Text($"{q.Number}.").Bold();
                row.RelativeItem().Text(q.Text);
                row.ConstantItem(60).AlignRight().Text($"({Points(q.Points)})").FontSize(9).FontColor(Muted);
            });

            if (Hint(q.Type) is { } hint)
                column.Item().PaddingLeft(24).Text(hint).FontSize(9).Italic().FontColor(Muted);

            switch (q.Type)
            {
                case QuestionType.Matching:
                    column.Item().PaddingLeft(24).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            foreach (var item in q.Options)
                                left.Item().Text($"{item.Label}. {item.Text}   ____");
                        });
                        row.ConstantItem(20);
                        row.RelativeItem().Column(right =>
                        {
                            foreach (var item in q.MatchItems)
                                right.Item().Text($"{item.Label}) {item.Text}");
                        });
                    });
                    break;

                case QuestionType.Ordering:
                    foreach (var item in q.Options)
                        column.Item().PaddingLeft(24).Text($"{item.Label}. {item.Text}");
                    column.Item().PaddingLeft(24).PaddingTop(2).Text("Correct order: ______________________");
                    break;

                default:
                    foreach (var option in q.Options)
                    {
                        var marker = q.Type == QuestionType.MultipleSelect ? "☐" : "○";
                        column.Item().PaddingLeft(24).Text($"{marker}  {option.Label}. {option.Text}");
                    }
                    break;
            }

            for (var i = 0; i < q.AnswerLines; i++)
                column.Item().PaddingLeft(24).PaddingTop(12).LineHorizontal(0.5f).LineColor(Rule);
        });
    }

    private static void Footer(IContainer container, ExamHeaderDto h)
    {
        container.BorderTop(0.5f).BorderColor(Rule).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text($"{h.CourseCode} · {h.Title}").FontSize(8).FontColor(Muted);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Muted));
                t.Span("Page ");
                t.CurrentPageNumber();
                t.Span(" of ");
                t.TotalPages();
            });
        });
    }
}
