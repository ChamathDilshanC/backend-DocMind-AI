using DocumentAssistant.Domain.Common;
using DocumentAssistant.Domain.Enums;

namespace DocumentAssistant.Domain.Entities;

public class Document : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DocumentFileType FileType { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public string? ProcessingError { get; set; }
    public int? PageCount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Chunk> Chunks { get; set; } = new List<Chunk>();
}
