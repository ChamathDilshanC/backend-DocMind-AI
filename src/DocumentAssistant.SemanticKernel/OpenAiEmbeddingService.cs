using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.AI;

namespace DocumentAssistant.SemanticKernel;

public class OpenAiEmbeddingService(KernelFactory kernelFactory) : IEmbeddingService
{
    // Lazy: building the Kernel touches the OpenAI connector immediately and throws if the API key
    // is missing/blank. Deferring that until first use keeps DI activation itself from failing, so
    // callers (e.g. a Hangfire job) can catch a clear error instead of the job silently never running.
    private readonly Lazy<IEmbeddingGenerator<string, Embedding<float>>> _embeddingGenerator =
        new(() => kernelFactory.CreateEmbeddingKernel().GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.Value.GenerateAsync(text, cancellationToken: cancellationToken);
        return embedding.Vector.ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingGenerator.Value.GenerateAsync(texts, cancellationToken: cancellationToken);
        return embeddings.Select(e => e.Vector.ToArray()).ToList();
    }
}
