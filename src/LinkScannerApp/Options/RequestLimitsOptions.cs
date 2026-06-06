namespace LinkScannerApp.Options;

public sealed class RequestLimitsOptions
{
    public const string SectionName = "RequestLimits";
    public long MaxScanRequestBodyBytes { get; init; } = 4096;
}