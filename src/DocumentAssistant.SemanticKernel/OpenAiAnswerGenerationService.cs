using System.Net;
using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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

    // Which model this key can actually use does not change while the process runs, but the
    // walk above was repeated for every question — so if the configured model 404s, each
    // answer paid a full round-trip per rejected candidate before generation even began,
    // and the fire-and-forget title generation paid it again. Remember the winner and start
    // from it; the rest of the chain stays available if it ever stops working.
    private volatile string? _resolvedGeminiModel;

    // No execution settings were sent at all, so an answer ran until the model chose to
    // stop. A grounded answer over five retrieved chunks does not need more than this,
    // and the cap bounds the worst case rather than trimming a typical reply.
    private const int MaxAnswerTokens = 1024;

    private bool UseGemini => string.Equals(openAiOptions.Value.Provider, "Gemini", StringComparison.OrdinalIgnoreCase);

    private PromptExecutionSettings ExecutionSettings => UseGemini
        ? new GeminiPromptExecutionSettings { MaxTokens = MaxAnswerTokens }
        : new OpenAIPromptExecutionSettings { MaxTokens = MaxAnswerTokens };

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
            var ordered = new[] { _resolvedGeminiModel, geminiOptions.Value.ChatModel }
                .Concat(GeminiChatFallbacks)
                .Where(m => !string.IsNullOrWhiteSpace(m));

            foreach (var model in ordered)
            {
                if (!candidates.Contains(model, StringComparer.OrdinalIgnoreCase)) candidates.Add(model);
            }
        }
        else
        {
            candidates.Add(null);
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            var chatCompletionService = kernelFactory.CreateChatKernel(candidates[i]).GetRequiredService<IChatCompletionService>();
            await using var enumerator = chatCompletionService
                .GetStreamingChatMessageContentsAsync(chatHistory, ExecutionSettings, cancellationToken: cancellationToken)
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

            if (unavailable is null)
            {
                if (UseGemini && !string.Equals(_resolvedGeminiModel, candidates[i], StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Using Gemini chat model {Model} for subsequent requests.", candidates[i]);
                    _resolvedGeminiModel = candidates[i];
                }

                yield break;
            }

            if (i == candidates.Count - 1) throw unavailable;
            logger.LogWarning("Chat model {Model} is not available (404); falling back to the next candidate.", candidates[i]);
        }
    }
}
