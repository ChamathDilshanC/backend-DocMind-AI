using DocumentAssistant.Domain.Enums;

namespace DocumentAssistant.Application.Common.Models;

public record FileValidationResult(bool IsValid, DocumentFileType? FileType, string? Error);

/// <summary>Validates uploaded files by extension AND magic bytes (never trust Content-Type alone).</summary>
public static class FileValidator
{
    public const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    private static readonly byte[] PdfMagicBytes = "%PDF"u8.ToArray();
    private static readonly byte[] ZipMagicBytes = [0x50, 0x4B, 0x03, 0x04]; // DOCX is a zip container

    public static async Task<FileValidationResult> ValidateAsync(string fileName, long fileSizeBytes, Stream content, CancellationToken cancellationToken = default)
    {
        if (fileSizeBytes <= 0 || fileSizeBytes > MaxFileSizeBytes)
        {
            return new FileValidationResult(false, null, $"File size must be between 1 byte and {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var declaredType = extension switch
        {
            ".pdf" => DocumentFileType.Pdf,
            ".docx" => DocumentFileType.Docx,
            _ => (DocumentFileType?)null
        };

        if (declaredType is null)
        {
            return new FileValidationResult(false, null, "Only .pdf and .docx files are supported.");
        }

        var header = new byte[4];
        var read = await content.ReadAsync(header.AsMemory(0, 4), cancellationToken);
        content.Seek(0, SeekOrigin.Begin);

        if (read < 4)
        {
            return new FileValidationResult(false, null, "File is empty or corrupt.");
        }

        var expectedMagic = declaredType == DocumentFileType.Pdf ? PdfMagicBytes : ZipMagicBytes;
        if (!header.AsSpan(0, expectedMagic.Length).SequenceEqual(expectedMagic))
        {
            return new FileValidationResult(false, null, "File content does not match its extension.");
        }

        return new FileValidationResult(true, declaredType, null);
    }
}
