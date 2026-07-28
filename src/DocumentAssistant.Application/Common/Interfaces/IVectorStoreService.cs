namespace DocumentAssistant.Application.Common.Interfaces;

public record VectorPoint(Guid Id, float[] Embedding, Guid UserId, Guid DocumentId, int Page, int ChunkIndex, string Filename, DateTime CreatedDate);

public record VectorSearchResult(Guid ChunkId, float Score, Guid DocumentId, int Page, int ChunkIndex, string Filename);

public interface IVectorStoreService
{
    Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default);
    Task UpsertBatchAsync(IEnumerable<VectorPoint> points, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding, Guid userId, Guid? documentId, int topK, CancellationToken cancellationToken = default);

    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
}
