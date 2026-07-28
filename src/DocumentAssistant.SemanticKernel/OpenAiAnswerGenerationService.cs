using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DocumentAssistant.SemanticKernel;

public class OpenAiAnswerGenerationService(KernelFactory kernelFactory) : IAnswerGenerationService
{
    private readonly IChatCompletionService _chatCompletionService =
        kernelFactory.CreateChatKernel().GetRequiredService<IChatCompletionService>();

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

        await foreach (var chunk in _chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return chunk.Content;
            }
        }
    }
}
