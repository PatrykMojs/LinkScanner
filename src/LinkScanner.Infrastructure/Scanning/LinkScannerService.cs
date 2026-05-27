using LinkScanner.Application.Abstractions;
using LinkScanner.Domain.Entities;
using LinkScanner.Infrastructure.Scanning.Analyzers;
using LinkScanner.Infrastructure.Scanning.Http;

namespace LinkScanner.Infrastructure.Scanning;

public sealed class LinkScannerService : ILinkScanner
{
    private readonly SecurityHeadersAnalyzer securityHeadersAnalyzer;
    private readonly HostIpResolver hostIpResolver;
    private readonly RiskScoreCalculator riskScoreCalculator;
    private readonly RedirectAnalyzer redirectAnalyzer;
    private readonly TlsCertificateAnalyzer tlsCertificateAnalyzer;
    private readonly HtmlMetadataExtractor htmlMetadataExtractor;
    private readonly HttpPageFetcher httpPageFetcher;

    public LinkScannerService(SecurityHeadersAnalyzer securityHeadersAnalyzer,
        HostIpResolver hostIpResolver,
        RiskScoreCalculator riskScoreCalculator,
        RedirectAnalyzer redirectAnalyzer,
        TlsCertificateAnalyzer tlsCertificateAnalyzer,
        HtmlMetadataExtractor htmlMetadataExtractor,
        HttpPageFetcher httpPageFetcher)
    {
        this.securityHeadersAnalyzer = securityHeadersAnalyzer;
        this.hostIpResolver = hostIpResolver;
        this.riskScoreCalculator = riskScoreCalculator;
        this.redirectAnalyzer = redirectAnalyzer;
        this.tlsCertificateAnalyzer = tlsCertificateAnalyzer;
        this.htmlMetadataExtractor = htmlMetadataExtractor;
        this.httpPageFetcher = httpPageFetcher;
    }

    public async Task<LinkScanResult> ScanAsync(string url, CancellationToken cancellationToken = default)
    {
        var result = new LinkScanResult { Url = url };

        try
        {
            result.HostIps = await hostIpResolver.ResolveAsync(url, cancellationToken);
            result.RedirectChain = await redirectAnalyzer.AnalyzeAsync(url, cancellationToken);

            var page = await httpPageFetcher.FetchAsync(url, cancellationToken);

            result.RawHeaders = page.RawHeaders;
            result.RawContentHeaders = page.RawContentHeaders;
            result.StatusCode = page.StatusCode;
            result.ContentType = page.ContentType;
            result.HttpVersion = page.HttpVersion;
            result.ServerHeader = page.ServerHeader;
            result.XPoweredBy = page.XPoweredBy;
            result.TtfbMs = page.TtfbMs;
            result.LoadTime = page.LoadTime;
            result.HtmlBytes = page.HtmlBytes;
            result.Headers = securityHeadersAnalyzer.Analyze(page.Response);

            var metadata = htmlMetadataExtractor.Extract(page.Html, url);

            result.Title = metadata.Title;
            result.Description = metadata.Description;
            result.CanonicalUrl = metadata.CanonicalUrl;
            result.FaviconUrl = metadata.FaviconUrl;
            result.LinksCount = metadata.LinksCount;
            result.ScriptsCount = metadata.ScriptsCount;
            result.ImageCount = metadata.ImageCount;
            result.MixedContent = metadata.MixedContent;

            var tls = await tlsCertificateAnalyzer.AnalyzeAsync(url, cancellationToken);

            if (tls is not null)
            {
                result.TlsProtocol = tls?.Protocol;
                result.CertSubject = tls?.Subject;
                result.CertIssuer = tls?.Issuer;
                result.CertNotAfter = tls?.NotAfter;
                result.CertDaysToExpiry = tls is null
                    ? null
                    : (int)(tls.NotAfter - DateTimeOffset.UtcNow).TotalDays;
            }

            result.IsSafe = !url.Contains("phishing", StringComparison.OrdinalIgnoreCase)
                && !url.Contains("malware", StringComparison.OrdinalIgnoreCase);

            result.RiskScore = riskScoreCalculator.Calculate(url, result);
        }
        catch
        {
            result.IsSafe = false;
            result.RiskScore = 90;
        }

        return result;
    }
}