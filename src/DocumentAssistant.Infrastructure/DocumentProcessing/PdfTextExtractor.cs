using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Enums;
using UglyToad.PdfPig;

namespace DocumentAssistant.Infrastructure.DocumentProcessing;

public class PdfTextExtractor : IDocumentTextExtractor
{
    public DocumentFileType SupportedFileType => DocumentFileType.Pdf;

    public Task<IReadOnlyList<ExtractedPage>> ExtractAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        using var document = PdfDocument.Open(fileStream);

        IReadOnlyList<ExtractedPage> pages = document.GetPages()
            .Select(page => new ExtractedPage(page.Number, TextCleaner.Clean(page.Text)))
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .ToList();

        return Task.FromResult(pages);
    }
}
