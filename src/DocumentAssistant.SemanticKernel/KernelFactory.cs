using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace DocumentAssistant.SemanticKernel;

/// <summary>Builds the Kernel instances used by the embedding and answer-generation services. Swapping to Gemini later
/// only requires new connector registrations here — the Application-layer interfaces never change.</summary>
public class KernelFactory(IOptions<OpenAIOptions> options)
{
    private readonly OpenAIOptions _options = options.Value;

    public Kernel CreateChatKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(_options.ChatModel, _options.ApiKey);
        return builder.Build();
    }

#pragma warning disable SKEXP0010
    public Kernel CreateEmbeddingKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIEmbeddingGenerator(_options.EmbeddingModel, _options.ApiKey);
        return builder.Build();
    }
#pragma warning restore SKEXP0010
}
