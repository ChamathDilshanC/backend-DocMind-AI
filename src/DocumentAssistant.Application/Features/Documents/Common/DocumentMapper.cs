using DocumentAssistant.Domain.Entities;

namespace DocumentAssistant.Application.Features.Documents.Common;

public static class DocumentMapper
{
    public static DocumentDto ToDto(Document d) => new(
        d.Id, d.Name, d.FileType.ToString(), d.FileSizeBytes, d.Status.ToString(), d.ProcessingError, d.PageCount, d.CreatedAt, d.UpdatedAt);
}
