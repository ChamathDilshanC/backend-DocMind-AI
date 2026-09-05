using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;

namespace DocumentAssistant.VectorStore;

/// <summary>
/// Idempotently ensures the Qdrant collection + payload indexes exist, then keeps the cluster warm.
///
/// This runs in the background instead of blocking startup on purpose. Ensuring the collection used
/// to happen inside IHostedService.StartAsync, so an unreachable Qdrant threw out of host startup
/// and killed the whole process — the API went down entirely (no login, no chat history) because of
/// one degraded dependency. Now the app boots and serves everything that does not need vectors while
/// this retries in the background.
///
/// The keep-alive ping is here because Qdrant Cloud free-tier clusters suspend after a stretch with
/// no requests. It only runs while this process is awake, so on a host that sleeps when idle it
/// complements — and does not replace — an external uptime ping (see DEPLOYMENT.md).
/// </summary>
public class QdrantWarmupService(
    IVectorStoreService vectorStoreService,
    QdrantClient client,
    IOptions<QdrantOptions> options,
    ILogger<QdrantWarmupService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(2);

    private readonly QdrantOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // An unhandled exception out of ExecuteAsync stops the host by default, which is exactly the
        // failure mode this service exists to prevent — so nothing is allowed to escape.
        try
        {
            await InitializeWithRetryAsync(stoppingToken);
            await KeepAliveAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Qdrant warmup service stopped unexpectedly; vector search may be unavailable");
        }
    }

    private async Task InitializeWithRetryAsync(CancellationToken cancellationToken)
    {
        var delay = InitialRetryDelay;

        for (var attempt = 1; !cancellationToken.IsCancellationRequested; attempt++)
        {
            try
            {
                await vectorStoreService.EnsureCollectionExistsAsync(cancellationToken);
                logger.LogInformation(
                    "Qdrant collection '{Collection}' is ready (attempt {Attempt})",
                    _options.CollectionName, attempt);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex,
                    "Qdrant initialization attempt {Attempt} failed; retrying in {Delay}. " +
                    "Vector search stays unavailable until this succeeds — check that the cluster is running",
                    attempt, delay);
            }

            await Task.Delay(delay, cancellationToken);
            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxRetryDelay.Ticks));
        }
    }

    private async Task KeepAliveAsync(CancellationToken cancellationToken)
    {
        if (_options.KeepAliveInterval <= TimeSpan.Zero)
        {
            return;
        }

        using var timer = new PeriodicTimer(_options.KeepAliveInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await client.ListCollectionsAsync(cancellationToken);
                logger.LogDebug("Qdrant keep-alive ping succeeded");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Qdrant keep-alive ping failed");
            }
        }
    }
}
