using DocumentAssistant.SemanticKernel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DocumentAssistant.API.HealthChecks;

/// <summary>
/// Checks that an AI provider is configured — not a live call, so it never burns rate-limited
/// GitHub Models quota (or OpenAI spend) just because something is polling /health.
/// </summary>
public class AiProviderHealthCheck(IOptions<OpenAIOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var config = options.Value;

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("OpenAI:ApiKey is not configured"));
        }

        var provider = string.IsNullOrWhiteSpace(config.Endpoint) ? "OpenAI" : "GitHub Models (custom endpoint)";
        return Task.FromResult(HealthCheckResult.Healthy($"Configured — {provider}, chat model {config.ChatModel}"));
    }
}
