using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Enums;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DocumentAssistant.Infrastructure.DocumentProcessing;

public class PdfTextExtractor : IDocumentTextExtractor
{
    public DocumentFileType SupportedFileType => DocumentFileType.Pdf;

    public Task<IReadOnlyList<ExtractedPage>> ExtractAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        using var document = PdfDocument.Open(fileStream);

        IReadOnlyList<ExtractedPage> pages = document.GetPages()
            .Select(page => new ExtractedPage(page.Number, TextCleaner.Clean(ExtractPageText(page))))
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .ToList();

        return Task.FromResult(pages);
    }

    // Page.Text concatenates glyph runs in content-stream order with no reliable word
    // spacing, so labels from diagrams, tables, and bullet lists (positioned via layout
    // rather than a literal space glyph) get glued into unreadable walls of run-on text.
    // GetWords() segments glyphs into words using gap-width heuristics, so joining those
    // with single spaces reconstructs real word boundaries instead.
    private static string ExtractPageText(Page page) => string.Join(' ', page.GetWords().Select(w => w.Text));
}
