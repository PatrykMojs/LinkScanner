using LinkScanner.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace LinkScanner.Infrastructure.Caching;

public sealed class InMemoryScanResultCache : IScanResultCache
{
    private readonly IMemoryCache _memoryCache;

    public InMemoryScanResultCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<ScanCacheEntry<T>?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
    {
        _memoryCache.TryGetValue(cacheKey, out ScanCacheEntry<T>? entry);

        return Task.FromResult(entry);
    }

    public Task SetAsync<T>(string cachekey, T result, DateTimeOffset scannedAt, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var cacheEntry = new ScanCacheEntry<T>
        {
            Result = result,
            ScannedAt = scannedAt,
            CacheExpiresAt = scannedAt.Add(ttl)
        };

        _memoryCache.Set(cachekey, cacheEntry, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });

        return Task.CompletedTask;
    }
}