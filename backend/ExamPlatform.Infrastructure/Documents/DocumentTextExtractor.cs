using System.Text;
using DocumentFormat.OpenXml.Packaging;
using ExamPlatform.Application.Common.Interfaces;
using UglyToad.PdfPig;
using A = DocumentFormat.OpenXml.Drawing;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace ExamPlatform.Infrastructure.Documents;

/// <summary>In-memory text extraction for .pdf (PdfPig), .docx and .pptx (OpenXML SDK).</summary>
public sealed class DocumentTextExtractor : IDocumentTextExtractor
{
    public Task<string> ExtractTextAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        var text = extension.ToLowerInvariant() switch
        {
            ".pdf" => ExtractPdf(content, cancellationToken),
            ".docx" => ExtractDocx(content),
            ".pptx" => ExtractPptx(content, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported file type '{extension}'.")
        };

        return Task.FromResult(Normalize(text));
    }

    private static string ExtractPdf(Stream content, CancellationToken cancellationToken)
    {
        using var pdf = PdfDocument.Open(content);
        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private static string ExtractDocx(Stream content)
    {
        using var doc = WordprocessingDocument.Open(content, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var paragraph in body.Descendants<W.Paragraph>())
            sb.AppendLine(paragraph.InnerText);
        return sb.ToString();
    }

    private static string ExtractPptx(Stream content, CancellationToken cancellationToken)
    {
        using var presentation = PresentationDocument.Open(content, false);
        var part = presentation.PresentationPart;
        var slideIds = part?.Presentation?.SlideIdList?.ChildElements.OfType<DocumentFormat.OpenXml.Presentation.SlideId>()
                       ?? [];

        var sb = new StringBuilder();
        var index = 1;
        foreach (var slideId in slideIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (slideId.RelationshipId?.Value is not { } relId || part!.GetPartById(relId) is not SlidePart slide)
                continue;

            sb.AppendLine($"Slide {index++}:");
            foreach (var paragraph in slide.Slide?.Descendants<A.Paragraph>() ?? [])
            {
                var line = string.Concat(paragraph.Descendants<A.Text>().Select(t => t.Text));
                if (!string.IsNullOrWhiteSpace(line))
                    sb.AppendLine(line);
            }
        }
        return sb.ToString();
    }

    private static string Normalize(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);
        return string.Join('\n', lines);
    }
}
