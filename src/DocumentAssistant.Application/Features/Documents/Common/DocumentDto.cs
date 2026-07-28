namespace DocumentAssistant.Application.Features.Documents.Common;

public record DocumentDto(
    Guid Id, string Name, string FileType, long FileSizeBytes,
    string Status, string? ProcessingError, int? PageCount, DateTime CreatedAt, DateTime UpdatedAt);
