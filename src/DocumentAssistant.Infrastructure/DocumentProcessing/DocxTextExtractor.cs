using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Enums;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocumentAssistant.Infrastructure.DocumentProcessing;

public class DocxTextExtractor : IDocumentTextExtractor
{
    public DocumentFileType SupportedFileType => DocumentFileType.Docx;

    public Task<IReadOnlyList<ExtractedPage>> ExtractAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        using var wordDocument = WordprocessingDocument.Open(fileStream, false);
        var body = wordDocument.MainDocumentPart?.Document?.Body
            ?? throw new InvalidOperationException("DOCX file has no document body.");

        var paragraphText = body.Descendants<Paragraph>().Select(p => p.InnerText);
        var text = TextCleaner.Clean(string.Join("\n", paragraphText));

        // DOCX has no stored pagination (it's a rendering-time concept), so the whole document is treated as page 1.
        IReadOnlyList<ExtractedPage> pages = string.IsNullOrWhiteSpace(text)
            ? []
            : [new ExtractedPage(1, text)];

        return Task.FromResult(pages);
    }
}
