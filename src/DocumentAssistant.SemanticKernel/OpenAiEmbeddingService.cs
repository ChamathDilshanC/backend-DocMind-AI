using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.AI;

namespace DocumentAssistant.SemanticKernel;

public class OpenAiEmbeddingService(KernelFactory kernelFactory) : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator =
        kernelFactory.CreateEmbeddingKernel().GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateAsync(text, cancellationToken: cancellationToken);
        return embedding.Vector.ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingGenerator.GenerateAsync(texts, cancellationToken: cancellationToken);
        return embeddings.Select(e => e.Vector.ToArray()).ToList();
    }
}
