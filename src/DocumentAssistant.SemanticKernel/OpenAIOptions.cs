namespace DocumentAssistant.SemanticKernel;

public class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>Which AI provider to use for chat and embeddings. Supported values: "OpenAI", "Gemini".</summary>
    public string Provider { get; set; } = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4o";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>Optional OpenAI-compatible custom endpoint (e.g. GitHub Models' free tier for dev/testing). Null = real OpenAI.</summary>
    public string? Endpoint { get; set; }
}

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gemini-2.5-flash";
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";

    /// <summary>Output dimensionality for Gemini embeddings (Matryoshka truncation: 768/1536/3072). Must match Qdrant:VectorSize.</summary>
    public int? EmbeddingDimensions { get; set; } = 768;
}
