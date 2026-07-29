namespace DocumentAssistant.VectorStore;

public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } = "localhost";
    public int GrpcPort { get; set; } = 6334;
    public bool UseHttps { get; set; }

    /// <summary>Required for Qdrant Cloud; leave null for a local/self-hosted instance with no auth configured.</summary>
    public string? ApiKey { get; set; }

    public string CollectionName { get; set; } = "document_chunks";
    public int VectorSize { get; set; } = 1536;
}
