using System.Net;
using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DocumentAssistant.SemanticKernel;

public class OpenAiAnswerGenerationService(
    KernelFactory kernelFactory,
    IOptions<OpenAIOptions> openAiOptions,
    IOptions<GeminiOptions> geminiOptions,
    ILogger<OpenAiAnswerGenerationService> logger) : IAnswerGenerationService
{
    // Google restricts older chat models (e.g. gemini-2.5-flash) for new API keys with a
    // 404, so when the configured model is unavailable we walk this chain until one works.
    private static readonly string[] GeminiChatFallbacks =
        ["gemini-3.5-flash", "gemini-flash-latest", "gemini-3-flash-preview", "gemini-2.5-flash"];

    private bool UseGemini => string.Equals(openAiOptions.Value.Provider, "Gemini", StringComparison.OrdinalIgnoreCase);

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

        // Configured model first, then known-available fallbacks (Gemini only; the OpenAI
        // path gets a single attempt with its configured model).
        var candidates = new List<string?>();
        if (UseGemini)
        {
            candidates.Add(geminiOptions.Value.ChatModel);
            candidates.AddRange(GeminiChatFallbacks.Where(m => !string.Equals(m, geminiOptions.Value.ChatModel, StringComparison.OrdinalIgnoreCase)));
        }
        else
        {
            candidates.Add(null);
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            var chatCompletionService = kernelFactory.CreateChatKernel(candidates[i]).GetRequiredService<IChatCompletionService>();
            await using var enumerator = chatCompletionService
                .GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            var yielded = false;
            HttpOperationException? unavailable = null;

            while (true)
            {
                StreamingChatMessageContent? current = null;
                try
                {
                    if (!await enumerator.MoveNextAsync()) break;
                    current = enumerator.Current;
                }
                catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.NotFound && !yielded)
                {
                    unavailable = ex;
                    break;
                }

                if (current is not null && !string.IsNullOrEmpty(current.Content))
                {
                    yielded = true;
                    yield return current.Content;
                }
            }

            if (unavailable is null) yield break;

            if (i == candidates.Count - 1) throw unavailable;
            logger.LogWarning("Chat model {Model} is not available (404); falling back to the next candidate.", candidates[i]);
        }
    }
}
