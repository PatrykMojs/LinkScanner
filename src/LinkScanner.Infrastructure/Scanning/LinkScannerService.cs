using LinkScanner.Application.Abstractions;
using LinkScanner.Domain.Entities;
using LinkScanner.Infrastructure.Scanning.Analyzers;
using LinkScanner.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace LinkScanner.Infrastructure.Scanning;

public sealed class LinkScannerService : ILinkScanner
{
    private readonly ILogger<LinkScannerService> _logger;
    private readonly SecurityHeadersAnalyzer _securityHeadersAnalyzer;
    private readonly HostIpResolver _hostIpResolver;
    private readonly RiskScoreCalculator _riskScoreCalculator;
    private readonly RedirectAnalyzer _redirectAnalyzer;
    private readonly TlsCertificateAnalyzer _tlsCertificateAnalyzer;
    private readonly HtmlMetadataExtractor _htmlMetadataExtractor;
    private readonly HttpPageFetcher _httpPageFetcher;
    private readonly SafetyDecisionAnalyzer _safetyDecisionAnalyzer;

    public LinkScannerService(ILogger<LinkScannerService> logger,
        SecurityHeadersAnalyzer securityHeadersAnalyzer,
        HostIpResolver hostIpResolver,
        RiskScoreCalculator riskScoreCalculator,
        RedirectAnalyzer redirectAnalyzer,
        TlsCertificateAnalyzer tlsCertificateAnalyzer,
        HtmlMetadataExtractor htmlMetadataExtractor,
        HttpPageFetcher httpPageFetcher,
        SafetyDecisionAnalyzer safetyDecisionAnalyzer)
    {
        _logger = logger;
        _securityHeadersAnalyzer = securityHeadersAnalyzer;
        _hostIpResolver = hostIpResolver;
        _riskScoreCalculator = riskScoreCalculator;
        _redirectAnalyzer = redirectAnalyzer;
        _tlsCertificateAnalyzer = tlsCertificateAnalyzer;
        _htmlMetadataExtractor = htmlMetadataExtractor;
        _httpPageFetcher = httpPageFetcher;
        _safetyDecisionAnalyzer = safetyDecisionAnalyzer;
    }

    public async Task<LinkScanResult> ScanAsync(string url, CancellationToken cancellationToken = default)
    {
        var result = new LinkScanResult { Url = url };
        _logger.LogInformation("Starting scan for URL: {Url}", url);

        try
        {
            result.HostIps = await _hostIpResolver.ResolveAsync(url, cancellationToken);
            _logger.LogDebug("Resolved IP addresses for URL {Url}: {IpCount}", url, result.HostIps.Count);

            result.RedirectChain = await _redirectAnalyzer.AnalyzeAsync(url, cancellationToken);
            _logger.LogDebug("Redirect chain analyzed for URL {Url}. Redirects count: {RedirectCount}", url, result.RedirectChain.Count);

            var page = await _httpPageFetcher.FetchAsync(url, cancellationToken);

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
            result.Headers = _securityHeadersAnalyzer.Analyze(page.Response);

            _logger.LogDebug("HTTP page fetched for URL {Url}. StatusCode: {StatusCode}, TTFB: {TtfbMs}ms", url, result.StatusCode, result.TtfbMs);

            var metadata = _htmlMetadataExtractor.Extract(page.Html, url);

            result.Title = metadata.Title;
            result.Description = metadata.Description;
            result.CanonicalUrl = metadata.CanonicalUrl;
            result.FaviconUrl = metadata.FaviconUrl;
            result.LinksCount = metadata.LinksCount;
            result.ScriptsCount = metadata.ScriptsCount;
            result.ImageCount = metadata.ImageCount;
            result.MixedContent = metadata.MixedContent;

            var tls = await _tlsCertificateAnalyzer.AnalyzeAsync(url, cancellationToken);

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

            result.RiskScore = _riskScoreCalculator.Calculate(url, result);
            result.IsSafe = _safetyDecisionAnalyzer.IsSafe(url, result);

            _logger.LogInformation("Finished scan for URL: {Url}. IsSafe: {IsSafe}, RiskScore: {RiskScore}, StatusCode: {StatusCode}", url, result.IsSafe, result.RiskScore, result.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed for URL: {Url}", url);

            result.IsSafe = false;
            result.RiskScore = 90;
        }

        return result;
    }
}