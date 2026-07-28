using DocumentAssistant.Domain.Enums;

namespace DocumentAssistant.Application.Common.Interfaces;

public record ExtractedPage(int PageNumber, string Text);

public interface IDocumentTextExtractor
{
    DocumentFileType SupportedFileType { get; }
    Task<IReadOnlyList<ExtractedPage>> ExtractAsync(Stream fileStream, CancellationToken cancellationToken = default);
}

public interface IDocumentTextExtractorFactory
{
    IDocumentTextExtractor GetExtractor(DocumentFileType fileType);
}
