namespace LinkScannerApp.Options;

public sealed class AppRateLimitOptions
{
    public const string SectionName = "RateLimiting";
    public int ScanPermitLimit { get; init; } = 10;
    public int WindowSeconds { get; init; } = 60;
    public int QueueLimit { get; init; } = 0;
}