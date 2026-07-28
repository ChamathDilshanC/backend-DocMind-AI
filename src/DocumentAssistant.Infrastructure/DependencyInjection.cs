using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Infrastructure.Auth;
using DocumentAssistant.Infrastructure.BackgroundJobs;
using DocumentAssistant.Infrastructure.Caching;
using DocumentAssistant.Infrastructure.DocumentProcessing;
using DocumentAssistant.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace DocumentAssistant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<GoogleAuthOptions>(configuration.GetSection(GoogleAuthOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddSingleton<IStorageService, LocalFileStorageService>();

        services.AddSingleton<IDocumentTextExtractor, PdfTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, DocxTextExtractor>();
        services.AddSingleton<IDocumentTextExtractorFactory, DocumentTextExtractorFactory>();
        services.AddSingleton<ITextChunker, SlidingWindowTextChunker>();

        services.AddScoped<IDocumentProcessingJob, DocumentProcessingJob>();
        services.AddScoped<IBackgroundJobEnqueuer, HangfireBackgroundJobEnqueuer>();

        var redisConnectionString = configuration.GetSection(RedisOptions.SectionName)["ConnectionString"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
