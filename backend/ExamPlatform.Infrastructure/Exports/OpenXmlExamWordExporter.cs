using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Features.Exams.Preview;
using ExamPlatform.Domain.Enums;
using static ExamPlatform.Infrastructure.Exports.ExportFormatting;

namespace ExamPlatform.Infrastructure.Exports;

/// <summary>Produces real Office Open XML (.docx) documents using the OpenXML SDK.</summary>
public sealed class OpenXmlExamWordExporter : IExamWordExporter
{
    private const string Muted = "6B7280";
    private const string RuleColor = "BFBFBF";
    private const int IndentTwips = 400;

    public byte[] ExportExam(ExamPreviewDto exam) => Build(exam.Header, isAnswerKey: false, body =>
    {
        body.Append(StudentFields());

        if (!string.IsNullOrWhiteSpace(exam.Header.Instructions))
        {
            body.Append(Para("Instructions", bold: true, spacingAfter: 0));
            body.Append(Para(exam.Header.Instructions!, spacingAfter: 200));
        }

        string? currentSection = null;
        foreach (var q in exam.Questions)
        {
            if (q.Section is { } section && section != currentSection)
            {
                currentSection = section;
                body.Append(Para(section, bold: true, size: 24, spacingBefore: 200, bottomBorder: true));
            }
            AppendQuestion(body, q);
        }
    });

    public byte[] ExportAnswerKey(ExamAnswerKeyDto answerKey) => Build(answerKey.Header, isAnswerKey: true, body =>
    {
        var table = new Table(TableBorders(), new TableRow(
            HeaderCell("Q#", 700), HeaderCell("Type", 1700), HeaderCell("Correct answer", 5600), HeaderCell("Marks", 900)));

        foreach (var item in answerKey.Items)
        {
            var answerCell = new TableCell(CellWidth(5600), Para(item.CorrectAnswer, spacingAfter: 0));
            if (!string.IsNullOrWhiteSpace(item.Explanation))
                answerCell.Append(Para(item.Explanation!, italic: true, size: 17, color: Muted, spacingAfter: 0));

            table.Append(new TableRow(
                new TableCell(CellWidth(700), Para(item.Number.ToString(), bold: true, spacingAfter: 0)),
                new TableCell(CellWidth(1700), Para(TypeLabel(item.Type), color: Muted, spacingAfter: 0)),
                answerCell,
                new TableCell(CellWidth(900), Para(Number(item.Points), align: JustificationValues.Right, spacingAfter: 0))));
        }

        table.Append(new TableRow(
            new TableCell(new TableCellProperties(new GridSpan { Val = 3 }), Para("Total", bold: true, align: JustificationValues.Right, spacingAfter: 0)),
            new TableCell(CellWidth(900), Para(Number(answerKey.Header.TotalPoints), bold: true, align: JustificationValues.Right, spacingAfter: 0))));

        body.Append(table);
    });

    private static byte[] Build(ExamHeaderDto header, bool isAnswerKey, Action<Body> writeContent)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body());
            var body = main.Document.Body!;

            AddDefaultStyles(main);
            AppendHeader(body, header, isAnswerKey);
            writeContent(body);

            var footerPart = main.AddNewPart<FooterPart>();
            footerPart.Footer = BuildFooter(header);
            var footerId = main.GetIdOfPart(footerPart);

            body.Append(new SectionProperties(
                new FooterReference { Type = HeaderFooterValues.Default, Id = footerId },
                new PageSize { Width = 11906U, Height = 16838U }, // A4
                new PageMargin { Top = 1134, Bottom = 1134, Left = 1134U, Right = 1134U, Header = 567U, Footer = 567U }));

            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static void AddDefaultStyles(MainDocumentPart main)
    {
        var stylesPart = main.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(new RunPropertiesBaseStyle(
                    new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", ComplexScript = "Arial" },
                    new FontSize { Val = "21" },
                    new FontSizeComplexScript { Val = "21" })),
                new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(
                    new SpacingBetweenLines { After = "80", Line = "276", LineRule = LineSpacingRuleValues.Auto }))));
    }

    private static void AppendHeader(Body body, ExamHeaderDto h, bool isAnswerKey)
    {
        body.Append(Para(h.UniversityName, bold: true, size: 30, align: JustificationValues.Center, spacingAfter: 0));
        body.Append(Para(h.DepartmentName, size: 22, color: Muted, align: JustificationValues.Center, spacingAfter: 0));
        body.Append(Para(CourseLine(h), size: 22, align: JustificationValues.Center, spacingAfter: 120));
        body.Append(Para(h.Title, bold: true, size: 34, align: JustificationValues.Center, spacingAfter: 0));
        body.Append(Para(isAnswerKey ? "ANSWER KEY — CONFIDENTIAL" : ExamTypeLabel(h.Type),
            bold: isAnswerKey, size: 20, color: isAnswerKey ? "B91C1C" : Muted, align: JustificationValues.Center, spacingAfter: 160));

        var info = new Table(
            new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 6, Color = RuleColor },
                    new BottomBorder { Val = BorderValues.Single, Size = 6, Color = RuleColor })),
            new TableRow(
                InfoCell("Date: ", Date(h.ExamDate), JustificationValues.Left),
                InfoCell("Duration: ", Duration(h.DurationMinutes), JustificationValues.Center),
                InfoCell("Total marks: ", Number(h.TotalPoints), JustificationValues.Right)));
        body.Append(info);
        body.Append(Para(string.Empty, spacingAfter: 120));
    }

    private static TableCell InfoCell(string label, string value, JustificationValues align)
    {
        var paragraph = new Paragraph(
            new ParagraphProperties(new Justification { Val = align }, new SpacingBetweenLines { Before = "60", After = "60" }),
            Run(label, bold: true), Run(value));
        return new TableCell(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "1666" }), paragraph);
    }

    private static OpenXmlElement[] StudentFields() =>
    [
        Para("Student Name: ______________________________________________", spacingBefore: 120, spacingAfter: 160),
        Para("Student ID: ____________________________        Signature: ____________________", spacingAfter: 240)
    ];

    private static void AppendQuestion(Body body, PreviewQuestionDto q)
    {
        var stem = new Paragraph(
            new ParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines { Before = "200", After = "60" },
                new Indentation { Left = IndentTwips.ToString(), Hanging = IndentTwips.ToString() }),
            Run($"{q.Number}.\t", bold: true),
            Run(q.Text),
            Run($"   ({Points(q.Points)})", size: 18, color: Muted));
        body.Append(stem);

        if (Hint(q.Type) is { } hint)
            body.Append(Para(hint, italic: true, size: 18, color: Muted, indent: IndentTwips, spacingAfter: 40, keepNext: true));

        switch (q.Type)
        {
            case QuestionType.Matching:
                var rows = Math.Max(q.Options.Count, q.MatchItems.Count);
                var table = new Table(new TableProperties(
                    new TableWidth { Type = TableWidthUnitValues.Pct, Width = "4700" },
                    new TableIndentation { Width = IndentTwips, Type = TableWidthUnitValues.Dxa }));
                for (var i = 0; i < rows; i++)
                {
                    var left = i < q.Options.Count ? $"{q.Options[i].Label}. {q.Options[i].Text}   ____" : string.Empty;
                    var right = i < q.MatchItems.Count ? $"{q.MatchItems[i].Label}) {q.MatchItems[i].Text}" : string.Empty;
                    table.Append(new TableRow(
                        new TableCell(CellWidth(4200), Para(left, spacingAfter: 40)),
                        new TableCell(CellWidth(4200), Para(right, spacingAfter: 40))));
                }
                body.Append(table);
                break;

            case QuestionType.Ordering:
                foreach (var item in q.Options)
                    body.Append(Para($"{item.Label}. {item.Text}", indent: IndentTwips, spacingAfter: 40));
                body.Append(Para("Correct order: ______________________", indent: IndentTwips, spacingAfter: 40));
                break;

            default:
                var marker = q.Type == QuestionType.MultipleSelect ? "☐" : "○";
                foreach (var option in q.Options)
                    body.Append(Para($"{marker}  {option.Label}. {option.Text}", indent: IndentTwips, spacingAfter: 40));
                break;
        }

        for (var i = 0; i < q.AnswerLines; i++)
            body.Append(Para(string.Empty, indent: IndentTwips, spacingBefore: 200, spacingAfter: 0, bottomBorder: true));
    }

    private static Footer BuildFooter(ExamHeaderDto h)
    {
        var paragraph = new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
            Run($"{h.CourseCode} · {h.Title}    Page ", size: 16, color: Muted),
            new SimpleField(Run("1", size: 16, color: Muted)) { Instruction = " PAGE " },
            Run(" of ", size: 16, color: Muted),
            new SimpleField(Run("1", size: 16, color: Muted)) { Instruction = " NUMPAGES " });
        return new Footer(paragraph);
    }

    private static TableProperties TableBorders() => new(
        new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
        new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4, Color = RuleColor },
            new BottomBorder { Val = BorderValues.Single, Size = 4, Color = RuleColor },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = RuleColor }),
        new TableCellMarginDefault(
            new TopMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
            new BottomMargin { Width = "60", Type = TableWidthUnitValues.Dxa }));

    private static TableCell HeaderCell(string text, int width) =>
        new(new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width.ToString() },
                new Shading { Val = ShadingPatternValues.Clear, Fill = "F3F4F6" }),
            Para(text, bold: true, spacingAfter: 0));

    private static TableCellProperties CellWidth(int width) =>
        new(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width.ToString() });

    private static Paragraph Para(
        string text,
        bool bold = false,
        bool italic = false,
        int? size = null,
        string? color = null,
        JustificationValues? align = null,
        int? indent = null,
        int spacingBefore = 0,
        int spacingAfter = 80,
        bool bottomBorder = false,
        bool keepNext = false)
    {
        var props = new ParagraphProperties();
        if (keepNext) props.Append(new KeepNext());
        if (bottomBorder)
            props.Append(new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 4, Color = RuleColor }));
        props.Append(new SpacingBetweenLines { Before = spacingBefore.ToString(), After = spacingAfter.ToString() });
        if (indent is { } left) props.Append(new Indentation { Left = left.ToString() });
        if (align is { } a) props.Append(new Justification { Val = a });

        return new Paragraph(props, Run(text, bold, italic, size, color));
    }

    private static Run Run(string text, bool bold = false, bool italic = false, int? size = null, string? color = null)
    {
        var props = new RunProperties();
        if (bold) props.Append(new Bold());
        if (italic) props.Append(new Italic());
        if (color is not null) props.Append(new Color { Val = color });
        if (size is { } s)
        {
            props.Append(new FontSize { Val = s.ToString() });
            props.Append(new FontSizeComplexScript { Val = s.ToString() });
        }

        var run = new Run(props);
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) run.Append(new Break());
            var parts = lines[i].Split('\t');
            for (var j = 0; j < parts.Length; j++)
            {
                if (j > 0) run.Append(new TabChar());
                run.Append(new Text(parts[j]) { Space = SpaceProcessingModeValues.Preserve });
            }
        }
        return run;
    }
}
