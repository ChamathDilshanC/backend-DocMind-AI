namespace DocumentAssistant.Application.Common.Interfaces;

public record ChatTurn(string Role, string Content);

public interface IAnswerGenerationService
{
    /// <summary>Streams the assistant's answer token-by-token given the RAG-built system prompt and prior turns.</summary>
    IAsyncEnumerable<string> StreamCompletionAsync(
        string systemPrompt, IReadOnlyList<ChatTurn> history, string question, CancellationToken cancellationToken = default);
}
