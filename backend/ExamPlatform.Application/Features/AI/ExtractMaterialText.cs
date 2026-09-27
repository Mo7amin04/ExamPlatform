using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Settings;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace ExamPlatform.Application.Features.AI;

public sealed record MaterialTextDto(string FileName, string Text, int CharacterCount, bool Truncated);

/// <summary>
/// Extracts text from an uploaded course material file so it can ground AI generation.
/// The file is processed in memory and never stored; no physical paths are exposed.
/// </summary>
public sealed record ExtractMaterialTextCommand(string FileName, long Length, Stream Content) : IRequest<MaterialTextDto>;

public sealed class ExtractMaterialTextCommandValidator : AbstractValidator<ExtractMaterialTextCommand>
{
    public ExtractMaterialTextCommandValidator(IOptions<FileUploadSettings> options)
    {
        var settings = options.Value;

        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255)
            .Must(name => settings.IsAllowedExtension(Path.GetExtension(name)))
            .WithMessage($"Only {string.Join(", ", settings.AllowedExtensions)} files are allowed.");
        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(settings.MaxFileSizeBytes)
            .WithMessage($"The file exceeds the maximum size of {settings.MaxFileSizeBytes / (1024 * 1024)} MB.");
    }
}

public sealed class ExtractMaterialTextCommandHandler(IDocumentTextExtractor extractor)
    : IRequestHandler<ExtractMaterialTextCommand, MaterialTextDto>
{
    public async Task<MaterialTextDto> Handle(ExtractMaterialTextCommand request, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();

        await using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        if (!FileSignatures.Matches(buffer, extension))
            throw new ValidationException("File", "The file content does not match its extension.");
        buffer.Position = 0;

        string text;
        try
        {
            text = await extractor.ExtractTextAsync(buffer, extension, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ValidationException("File", "The file could not be read. It may be corrupted or password protected.");
        }

        var max = GenerateQuestionsCommand.MaxSourceMaterialLength;
        var truncated = text.Length > max;
        if (truncated)
            text = text[..max];

        var safeName = Path.GetFileName(request.FileName);
        return new MaterialTextDto(safeName, text, text.Length, truncated);
    }
}

internal static class FileSignatures
{
    private static readonly byte[] Pdf = "%PDF"u8.ToArray();
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04];

    public static bool Matches(Stream stream, string extension)
    {
        Span<byte> header = stackalloc byte[4];
        if (stream.Read(header) < 4)
            return false;

        return extension switch
        {
            ".pdf" => header.SequenceEqual(Pdf),
            ".docx" or ".pptx" => header.SequenceEqual(Zip),
            _ => false
        };
    }
}
