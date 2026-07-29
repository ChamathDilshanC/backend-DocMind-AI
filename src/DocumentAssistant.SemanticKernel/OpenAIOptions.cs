namespace DocumentAssistant.SemanticKernel;

public class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4o";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>Optional OpenAI-compatible custom endpoint (e.g. GitHub Models' free tier for dev/testing). Null = real OpenAI.</summary>
    public string? Endpoint { get; set; }
}
