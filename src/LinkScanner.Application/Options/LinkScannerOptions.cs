namespace LinkScanner.Application.Options;

public sealed class LinkScannerOptions
{
    public const string SectionName = "LinkScanner";
    public int HttpTimeoutSeconds { get; init; } = 8;
    public int MaxRedirects { get; init; } = 5;
    public int MaxHtmlBytes { get; init; } = 1_000_000;
    public int MaxUrlLength { get; init; } = 2048;
    public int[] AllowedPorts { get; init; } = [80, 443];
    public int CacheTtlMinutes { get; init; } = 10;
    public int MaxConcurrentScans { get; init; } = 3;
}