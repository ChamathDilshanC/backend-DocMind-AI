using System.ClientModel;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OpenAI;

namespace DocumentAssistant.SemanticKernel;

/// <summary>Builds the Kernel instances used by the embedding and answer-generation services. Swapping to Gemini later
/// only requires new connector registrations here — the Application-layer interfaces never change.</summary>
public class KernelFactory(IOptions<OpenAIOptions> options)
{
    private readonly OpenAIOptions _options = options.Value;

    public Kernel CreateChatKernel()
    {
        var builder = Kernel.CreateBuilder();

        var customClient = CreateCustomEndpointClient();
        if (customClient is not null)
        {
            builder.AddOpenAIChatCompletion(_options.ChatModel, customClient);
        }
        else
        {
            builder.AddOpenAIChatCompletion(_options.ChatModel, _options.ApiKey);
        }

        return builder.Build();
    }

#pragma warning disable SKEXP0010
    public Kernel CreateEmbeddingKernel()
    {
        var builder = Kernel.CreateBuilder();

        var customClient = CreateCustomEndpointClient();
        if (customClient is not null)
        {
            builder.AddOpenAIEmbeddingGenerator(_options.EmbeddingModel, customClient);
        }
        else
        {
            builder.AddOpenAIEmbeddingGenerator(_options.EmbeddingModel, _options.ApiKey);
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
        if (string.IsNullOrWhiteSpace(_options.Endpoint)) return null;

        return new OpenAIClient(
            new ApiKeyCredential(_options.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(_options.Endpoint) });
    }
}
