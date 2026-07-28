using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentAssistant.SemanticKernel;

public static class DependencyInjection
{
    public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAIOptions>(configuration.GetSection(OpenAIOptions.SectionName));

        services.AddSingleton<KernelFactory>();
        services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddSingleton<IAnswerGenerationService, OpenAiAnswerGenerationService>();
        services.AddSingleton<IPromptBuilder, PromptBuilder>();

        return services;
    }
}
