namespace LinkScanner.Infrastructure.Scanning.Http;

public sealed class HttpPageFetchResult
{
    public int StatusCode { get; init; }
    public string? ContentType { get; init; }
    public string HttpVersion { get; init; } = string.Empty;
    public string? ServerHeader { get; init; }
    public string? XPoweredBy { get; init; }
    public int TtfbMs { get; init; }
    public TimeSpan LoadTime { get; init; }
    public int HtmlBytes { get; init; }
    public string Html { get; init; } = string.Empty;

    public Dictionary<string, string> RawHeaders { get; init; } = [];
    public Dictionary<string, string> RawContentHeaders { get; init; } = [];

    public HttpResponseMessage Response { get; init; } = default!;
}