using LinkScanner.Application.Abstractions;
using LinkScanner.Application.Abstractions.Caching;
using LinkScanner.Application.Abstractions.Scanning;
using LinkScanner.Application.Options;
using LinkScanner.Domain.Entities;
using LinkScanner.Infrastructure.Scanning.Analyzers;
using LinkScanner.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

    private readonly IScanResultCache _scanResultCache;
    private readonly LinkScannerOptions _options;
    private readonly IScanConcurrencyLimiter _scanConcurrencyLimiter;

    public LinkScannerService(ILogger<LinkScannerService> logger,
        SecurityHeadersAnalyzer securityHeadersAnalyzer,
        HostIpResolver hostIpResolver,
        RiskScoreCalculator riskScoreCalculator,
        RedirectAnalyzer redirectAnalyzer,
        TlsCertificateAnalyzer tlsCertificateAnalyzer,
        HtmlMetadataExtractor htmlMetadataExtractor,
        HttpPageFetcher httpPageFetcher,
        SafetyDecisionAnalyzer safetyDecisionAnalyzer, 
        IScanResultCache scanResultCache, 
        IOptions<LinkScannerOptions> options,
        IScanConcurrencyLimiter scanConcurrencyLimiter)
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
        _scanResultCache = scanResultCache;
        _options = options.Value;
        _scanConcurrencyLimiter = scanConcurrencyLimiter;
    }

    public async Task<LinkScanResult> ScanAsync(string url, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(url);
        var cached = await _scanResultCache.GetAsync<LinkScanResult>(cacheKey, cancellationToken);

        if(cached is not null)
        {
            _logger.LogInformation("Returning cached scan result for URL: {Url}. ScannedAt: {ScannedAt}, CacheExpiresAt: {CacheExpiresAt}",
                url,
                cached.ScannedAt,
                cached.CacheExpiresAt);

            var cachedResult = CloneResult(cached.Result);

            cachedResult.FromCache = true;
            cachedResult.ScannedAt = cached.ScannedAt;
            cachedResult.CacheExpiresAt = cached.CacheExpiresAt;

            return cachedResult;
        }

        using var scanLease = await _scanConcurrencyLimiter.TryAcquireAsync(cancellationToken);

        if(scanLease is null)
        {
            _logger.LogWarning("Scan rejected because the maximum number of concurrent scans has been reached. Url: {Url}", url);

            return new LinkScanResult
            {
                Url = url,
                IsSafe = false,
                RiskScore = 90,
                FromCache = false,
                ScannedAt = DateTimeOffset.UtcNow,
                CacheExpiresAt = null
            };
        }

        var scannedAt = DateTimeOffset.UtcNow;
        var cacheTtl = TimeSpan.FromMinutes(_options.CacheTtlMinutes);

        var result = new LinkScanResult 
        { 
            Url = url,
            FromCache = false,
            ScannedAt = scannedAt,
            CacheExpiresAt = scannedAt.Add(cacheTtl) 
        };

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

            await _scanResultCache.SetAsync(cacheKey, CloneResult(result), scannedAt, cacheTtl, cancellationToken);

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

    private static string BuildCacheKey(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return $"scan:{url.Trim().ToLowerInvariant()}";
        }

        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant()
        };

        return $"scan:{builder.Uri.AbsoluteUri}";
    }

    private static LinkScanResult CloneResult(LinkScanResult source)
    {
        return new LinkScanResult
        {
            Url = source.Url,
            IsSafe = source.IsSafe,
            RiskScore = source.RiskScore,

            StatusCode = source.StatusCode,
            ContentType = source.ContentType,
            HttpVersion = source.HttpVersion,
            ServerHeader = source.ServerHeader,
            XPoweredBy = source.XPoweredBy,
            TtfbMs = source.TtfbMs,
            LoadTime = source.LoadTime,
            HtmlBytes = source.HtmlBytes,

            Title = source.Title,
            Description = source.Description,
            CanonicalUrl = source.CanonicalUrl,
            FaviconUrl = source.FaviconUrl,
            LinksCount = source.LinksCount,
            ScriptsCount = source.ScriptsCount,
            ImageCount = source.ImageCount,
            MixedContent = source.MixedContent,

            TlsProtocol = source.TlsProtocol,
            CertSubject = source.CertSubject,
            CertIssuer = source.CertIssuer,
            CertNotAfter = source.CertNotAfter,
            CertDaysToExpiry = source.CertDaysToExpiry,

            FromCache = source.FromCache,
            ScannedAt = source.ScannedAt,
            CacheExpiresAt = source.CacheExpiresAt,

            HostIps = source.HostIps.ToList(),
            RedirectChain = source.RedirectChain.ToList(),
            Headers = source.Headers,

            RawHeaders = source.RawHeaders is not null
                ? source.RawHeaders.ToDictionary(x => x.Key, x => x.Value)
                : new Dictionary<string, string>(),

            RawContentHeaders = source.RawContentHeaders is not null
                ? source.RawContentHeaders.ToDictionary(x => x.Key, x => x.Value)
                : new Dictionary<string, string>()
        };
    }
}