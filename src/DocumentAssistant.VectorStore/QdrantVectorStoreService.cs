using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using static Qdrant.Client.Grpc.Conditions;

namespace DocumentAssistant.VectorStore;

public class QdrantVectorStoreService(QdrantClient client, IOptions<QdrantOptions> options) : IVectorStoreService
{
    private readonly QdrantOptions _options = options.Value;

    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        var exists = (await client.ListCollectionsAsync(cancellationToken)).Contains(_options.CollectionName);

        if (exists)
        {
            // A collection built for a different embedding model has incompatible vector
            // dimensions (e.g. 1536 for OpenAI vs 768 for Gemini), which makes every upsert
            // fail. Recreate it so the current model's dimensions work; vectors from another
            // model are useless for search anyway.
            var info = await client.GetCollectionInfoAsync(_options.CollectionName, cancellationToken);
            if (info.Config.Params.VectorsConfig.Params?.Size != (ulong)_options.VectorSize)
            {
                await client.DeleteCollectionAsync(_options.CollectionName, cancellationToken: cancellationToken);
                exists = false;
            }
        }

        if (!exists)
        {
            await client.CreateCollectionAsync(
                _options.CollectionName,
                new VectorParams { Size = (ulong)_options.VectorSize, Distance = Distance.Cosine },
                cancellationToken: cancellationToken);
        }

        await client.CreatePayloadIndexAsync(_options.CollectionName, "user_id", PayloadSchemaType.Keyword, cancellationToken: cancellationToken);
        await client.CreatePayloadIndexAsync(_options.CollectionName, "document_id", PayloadSchemaType.Keyword, cancellationToken: cancellationToken);
    }

    public async Task UpsertBatchAsync(IEnumerable<VectorPoint> points, CancellationToken cancellationToken = default)
    {
        var pointStructs = points.Select(p => new PointStruct
        {
            Id = p.Id,
            Vectors = p.Embedding,
            Payload =
            {
                ["user_id"] = p.UserId.ToString(),
                ["document_id"] = p.DocumentId.ToString(),
                ["page"] = p.Page,
                ["chunk_index"] = p.ChunkIndex,
                ["filename"] = p.Filename,
                ["created_date"] = p.CreatedDate.ToString("O")
            }
        }).ToList();

        if (pointStructs.Count == 0) return;

        await client.UpsertAsync(_options.CollectionName, pointStructs, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding, Guid userId, Guid? documentId, int topK, CancellationToken cancellationToken = default)
    {
        var filterConditions = new List<Condition> { MatchKeyword("user_id", userId.ToString()) };
        if (documentId is not null)
        {
            filterConditions.Add(MatchKeyword("document_id", documentId.Value.ToString()));
        }

        var filter = new Filter();
        filter.Must.AddRange(filterConditions);

        var results = await client.QueryAsync(
            _options.CollectionName,
            queryEmbedding,
            filter: filter,
            limit: (ulong)topK,
            cancellationToken: cancellationToken);

        return results.Select(r => new VectorSearchResult(
            ChunkId: Guid.Parse(r.Id.Uuid),
            Score: r.Score,
            DocumentId: Guid.Parse(r.Payload["document_id"].StringValue),
            Page: (int)r.Payload["page"].IntegerValue,
            ChunkIndex: (int)r.Payload["chunk_index"].IntegerValue,
            Filename: r.Payload["filename"].StringValue)).ToList();
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await client.DeleteAsync(
            _options.CollectionName,
            filter: MatchKeyword("document_id", documentId.ToString()),
            cancellationToken: cancellationToken);
    }
}
