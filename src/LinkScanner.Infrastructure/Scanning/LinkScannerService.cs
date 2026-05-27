using HtmlAgilityPack;
using System.Diagnostics;
using LinkScanner.Application.Abstractions;
using LinkScanner.Domain.Entities;
using LinkScanner.Infrastructure.Scanning.Analyzers;

namespace LinkScanner.Infrastructure.Scanning;

public sealed class LinkScannerService : ILinkScanner
{
    private readonly HttpClient httpClient;
    private readonly IUrlSafetyValidator urlSafetyValidator;
    private readonly SecurityHeadersAnalyzer securityHeadersAnalyzer;
    private readonly HostIpResolver hostIpResolver;
    private readonly RiskScoreCalculator riskScoreCalculator;
    private readonly RedirectAnalyzer redirectAnalyzer;
    private readonly TlsCertificateAnalyzer tlsCertificateAnalyzer;
    private readonly HtmlMetadataExtractor htmlMetadataExtractor;

    public LinkScannerService(HttpClient httpClient, 
        IUrlSafetyValidator urlSafetyValidator,
        SecurityHeadersAnalyzer securityHeadersAnalyzer,
        HostIpResolver hostIpResolver,
        RiskScoreCalculator riskScoreCalculator,
        RedirectAnalyzer redirectAnalyzer,
        TlsCertificateAnalyzer tlsCertificateAnalyzer,
        HtmlMetadataExtractor htmlMetadataExtractor)
    {
        this.httpClient = httpClient;
        this.urlSafetyValidator = urlSafetyValidator;
        this.securityHeadersAnalyzer = securityHeadersAnalyzer;
        this.hostIpResolver = hostIpResolver;
        this.riskScoreCalculator = riskScoreCalculator;
        this.redirectAnalyzer = redirectAnalyzer;
        this.tlsCertificateAnalyzer = tlsCertificateAnalyzer;
        this.htmlMetadataExtractor = htmlMetadataExtractor;

        this.httpClient.Timeout = TimeSpan.FromSeconds(8);
        this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LinkScannerApp/1.0");
    }

    public async Task<LinkScanResult> ScanAsync(string url, CancellationToken cancellationToken = default)
    {
        var validation = await urlSafetyValidator.ValidateAsync(url);

        if (!validation.IsValid)
        {
            return new LinkScanResult
            {
                Url = url,
                IsSafe = false,
                RiskScore = 100,
                StatusCode = null,
                Title = validation.ErrorMessage
            };
        }

        var result = new LinkScanResult { Url = url };

        try
        {
            result.HostIps = await hostIpResolver.ResolveAsync(url, cancellationToken);

            result.RedirectChain = await redirectAnalyzer.AnalyzeAsync(url, cancellationToken);

            var ttfb = new Stopwatch();
            string html;
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                ttfb.Start();
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                result.RawHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
                result.RawContentHeaders = response.Content.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
                ttfb.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ContentType = response.Content.Headers.ContentType?.MediaType;
                result.HttpVersion = response.Version.ToString();
                result.ServerHeader = response.Headers.TryGetValues("server", out var sv) ? sv.FirstOrDefault() : null;
                result.XPoweredBy = response.Headers.TryGetValues("x-powered-by", out var xp) ? xp.FirstOrDefault() : null;
                result.TtfbMs = (int)Math.Round(ttfb.Elapsed.TotalMilliseconds);

                var stopWatch = Stopwatch.StartNew();
                html = await response.Content.ReadAsStringAsync();
                stopWatch.Stop();
                result.LoadTime = stopWatch.Elapsed;
                result.HtmlBytes = System.Text.Encoding.UTF8.GetByteCount(html);

                result.Headers = securityHeadersAnalyzer.Analyze(response);
            }

            var metadata = htmlMetadataExtractor.Extract(html, url);

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