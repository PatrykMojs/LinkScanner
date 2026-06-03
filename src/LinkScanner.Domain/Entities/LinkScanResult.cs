namespace LinkScanner.Domain.Entities;

public sealed class LinkScanResult
{
    public string Url { get; set; } = "";
    public bool IsSafe { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? FaviconUrl { get; set; }

    public TimeSpan? LoadTime { get; set; }
    public int? StatusCode { get; set; }
    public string? ContentType { get; set; }
    public string? HttpVersion { get; set; }
    public int? HtmlBytes { get; set; }
    public int? TtfbMs { get; set; }

    public List<string> RedirectChain { get; set; } = new();
    public string? ServerHeader { get; set; }
    public string? XPoweredBy { get; set; }

    public List<string> HostIps { get; set; } = new();

    public string? TlsProtocol { get; set; }
    public string? CertSubject { get; set; }
    public string? CertIssuer { get; set; }
    public DateTimeOffset? CertNotAfter { get; set; }
    public int? CertDaysToExpiry { get; set; }

    public SecurityHeaders Headers { get; set; } = new();

    public string? CanonicalUrl { get; set; }
    public bool MixedContent { get; set; }
    public int LinksCount { get; set; }
    public int ScriptsCount { get; set; }
    public int ImageCount { get; set; }

    public int RiskScore { get; set; }

    public Dictionary<string, string>? RawHeaders { get; set; } = new();
    public Dictionary<string, string>? RawContentHeaders { get; set; } = new();

    public bool FromCache { get; set; }
    public DateTimeOffset ScannedAt { get; set; }
    public DateTimeOffset? CacheExpiresAt { get; set; }
}
