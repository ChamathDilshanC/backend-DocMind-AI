using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Enums;

namespace DocumentAssistant.Infrastructure.DocumentProcessing;

public class DocumentTextExtractorFactory(IEnumerable<IDocumentTextExtractor> extractors) : IDocumentTextExtractorFactory
{
    public IDocumentTextExtractor GetExtractor(DocumentFileType fileType) =>
        extractors.FirstOrDefault(e => e.SupportedFileType == fileType)
            ?? throw new NotSupportedException($"No text extractor registered for file type '{fileType}'.");
}
