using DocumentAssistant.Domain.Common;

namespace DocumentAssistant.Domain.Entities;

public class Chunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public int ChunkIndex { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>Qdrant point id for this chunk's embedding. Equal to Id, kept as a separate field to mirror the spec's Chunks table shape.</summary>
    public Guid EmbeddingId { get; set; }

    public int PageNumber { get; set; }
    public int TokenCount { get; set; }
}
