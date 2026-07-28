using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DocumentAssistant.SemanticKernel;

public class OpenAiAnswerGenerationService(KernelFactory kernelFactory) : IAnswerGenerationService
{
    // Lazy for the same reason as OpenAiEmbeddingService: defer the OpenAI connector's
    // key validation until first use so DI activation never fails outside a try/catch.
    private readonly Lazy<IChatCompletionService> _chatCompletionService =
        new(() => kernelFactory.CreateChatKernel().GetRequiredService<IChatCompletionService>());

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        string systemPrompt, IReadOnlyList<ChatTurn> history, string question,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory(systemPrompt);

        foreach (var turn in history)
        {
            if (turn.Role == "Assistant")
            {
                chatHistory.AddAssistantMessage(turn.Content);
            }
            else
            {
                chatHistory.AddUserMessage(turn.Content);
            }
        }

        chatHistory.AddUserMessage(question);

        await foreach (var chunk in _chatCompletionService.Value.GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return chunk.Content;
            }
        }
    }
}
