namespace DocumentAssistant.Application.Common.Interfaces;

public record SourceChunk(string Filename, int Page, string Text);

public interface IPromptBuilder
{
    /// <summary>Builds the RAG system prompt (spec template + context) from the top retrieved chunks, truncated to a token budget.</summary>
    string BuildSystemPrompt(IReadOnlyList<SourceChunk> sources);
}
