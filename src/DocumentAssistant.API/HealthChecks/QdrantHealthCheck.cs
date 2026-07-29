using Microsoft.Extensions.Diagnostics.HealthChecks;
using Qdrant.Client;

namespace DocumentAssistant.API.HealthChecks;

public class QdrantHealthCheck(QdrantClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await client.ListCollectionsAsync(cancellationToken);
            return HealthCheckResult.Healthy($"Reachable, {collections.Count} collection(s)");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Qdrant unreachable", ex);
        }
    }
}
