namespace LinkScanner.Application.Abstractions.Caching;

public interface IScanResultCache
{
    Task<ScanCacheEntry<T>?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string cachekey, T result, DateTimeOffset scannedAt, TimeSpan ttl, CancellationToken cancellationToken = default);
}