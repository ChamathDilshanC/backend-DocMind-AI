using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace DocumentAssistant.Infrastructure.Caching;

/// <summary>Placeholder ICacheService backed by in-process memory. Replaced by RedisCacheService once Redis is wired.</summary>
public class InMemoryCacheService(IMemoryCache memoryCache) : ICacheService
{
    private static readonly HashSet<string> Keys = [];

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(memoryCache.TryGetValue(key, out T? value) ? value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        memoryCache.Set(key, value, expiry);
        lock (Keys) Keys.Add(key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        List<string> matches;
        lock (Keys) matches = Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();

        foreach (var key in matches)
        {
            memoryCache.Remove(key);
            lock (Keys) Keys.Remove(key);
        }

        return Task.CompletedTask;
    }
}
