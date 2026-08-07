using DocumentAssistant.SemanticKernel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DocumentAssistant.API.HealthChecks;

/// <summary>
/// Checks that an AI provider is configured — not a live call, so it never burns rate-limited
/// quota (or paid spend) just because something is polling /health.
/// </summary>
public class AiProviderHealthCheck(
    IOptions<OpenAIOptions> openAiOptions,
    IOptions<GeminiOptions> geminiOptions) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var openAi = openAiOptions.Value;
        var gemini = geminiOptions.Value;

        if (string.Equals(openAi.Provider, "Gemini", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(gemini.ApiKey))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Gemini:ApiKey is not configured"));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Configured — Gemini, chat model {gemini.ChatModel}, embedding model {gemini.EmbeddingModel}"));
        }

        // OpenAI provider (default)
        if (string.IsNullOrWhiteSpace(openAi.ApiKey))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("OpenAI:ApiKey is not configured"));
        }

        var provider = string.IsNullOrWhiteSpace(openAi.Endpoint) ? "OpenAI" : "GitHub Models (custom endpoint)";
        return Task.FromResult(HealthCheckResult.Healthy($"Configured — {provider}, chat model {openAi.ChatModel}"));
    }
}
