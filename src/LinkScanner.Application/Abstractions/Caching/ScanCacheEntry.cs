namespace LinkScanner.Application.Abstractions.Caching;

public sealed class ScanCacheEntry<T>
{
    public required T Result { get; init; }
    public DateTimeOffset ScannedAt { get; init; }
    public DateTimeOffset CacheExpiresAt { get; init; }
}