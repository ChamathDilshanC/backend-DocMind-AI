using System.ClientModel;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using OpenAI;

namespace DocumentAssistant.SemanticKernel;

/// <summary>Builds the Kernel instances used by the embedding and answer-generation services.
/// Supports both OpenAI (including GitHub Models via custom endpoint) and Google Gemini —
/// selected via OpenAI:Provider. Swapping providers only requires a config change; the
/// Application-layer interfaces never change.</summary>
public class KernelFactory(IOptions<OpenAIOptions> openAiOptions, IOptions<GeminiOptions> geminiOptions)
{
    private readonly OpenAIOptions _openAi = openAiOptions.Value;
    private readonly GeminiOptions _gemini = geminiOptions.Value;

    private bool UseGemini => string.Equals(_openAi.Provider, "Gemini", StringComparison.OrdinalIgnoreCase);

    public Kernel CreateChatKernel()
    {
        var builder = Kernel.CreateBuilder();

        if (UseGemini)
        {
            builder.AddGoogleAIGeminiChatCompletion(_gemini.ChatModel, _gemini.ApiKey, GoogleAIVersion.V1);
        }
        else
        {
            var customClient = CreateCustomEndpointClient();
            if (customClient is not null)
            {
                builder.AddOpenAIChatCompletion(_openAi.ChatModel, customClient);
            }
            else
            {
                builder.AddOpenAIChatCompletion(_openAi.ChatModel, _openAi.ApiKey);
            }
        }

        return builder.Build();
    }

#pragma warning disable SKEXP0010
    public Kernel CreateEmbeddingKernel()
    {
        var builder = Kernel.CreateBuilder();

        if (UseGemini)
        {
            builder.AddGoogleAIEmbeddingGenerator(_gemini.EmbeddingModel, _gemini.ApiKey, GoogleAIVersion.V1);
        }
        else
        {
            var customClient = CreateCustomEndpointClient();
            if (customClient is not null)
            {
                builder.AddOpenAIEmbeddingGenerator(_openAi.EmbeddingModel, customClient);
            }
            else
            {
                builder.AddOpenAIEmbeddingGenerator(_openAi.EmbeddingModel, _openAi.ApiKey);
            }
        }

        return builder.Build();
    }
#pragma warning restore SKEXP0010

    /// <summary>
    /// Non-null only when OpenAI:Endpoint is configured (e.g. GitHub Models' free tier for dev/testing) — otherwise
    /// callers fall back to the standard (modelId, apiKey) overloads that talk to the real OpenAI API.
    /// </summary>
    private OpenAIClient? CreateCustomEndpointClient()
    {
        if (string.IsNullOrWhiteSpace(_openAi.Endpoint)) return null;

        return new OpenAIClient(
            new ApiKeyCredential(_openAi.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(_openAi.Endpoint) });
    }
}
