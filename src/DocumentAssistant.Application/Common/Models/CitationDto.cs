namespace DocumentAssistant.Application.Common.Models;

public record CitationDto(Guid DocumentId, string Filename, int Page, string ChunkExcerpt);
