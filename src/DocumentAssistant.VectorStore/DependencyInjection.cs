using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;

namespace DocumentAssistant.VectorStore;

public static class DependencyInjection
{
    public static IServiceCollection AddVectorStore(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(QdrantOptions.SectionName);
        services.Configure<QdrantOptions>(section);

        var qdrantOptions = section.Get<QdrantOptions>() ?? new QdrantOptions();

        services.AddSingleton(_ => new QdrantClient(
            qdrantOptions.Host, qdrantOptions.GrpcPort, qdrantOptions.UseHttps, qdrantOptions.ApiKey));
        services.AddSingleton<IVectorStoreService, QdrantVectorStoreService>();
        services.AddHostedService<QdrantCollectionInitializer>();

        return services;
    }
}
