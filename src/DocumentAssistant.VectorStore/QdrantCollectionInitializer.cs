using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace DocumentAssistant.VectorStore;

/// <summary>Idempotently ensures the Qdrant collection + payload indexes exist before the app starts serving traffic.</summary>
public class QdrantCollectionInitializer(IVectorStoreService vectorStoreService) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => vectorStoreService.EnsureCollectionExistsAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
