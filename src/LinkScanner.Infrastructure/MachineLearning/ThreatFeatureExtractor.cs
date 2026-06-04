using System.Net;
using LinkScanner.Application.Abstractions;
using LinkScanner.Application.ThreatIntelligence;
using LinkScanner.Domain.Entities;

namespace LinkScanner.Infrastructure.MachineLearning;

public sealed class ThreatFeatureExtractor : IThreatFeatureExtractor
{
    private static readonly string[] SuspiciousKeywords =
    [
        "login",
        "verify",
        "account",
        "secure",
        "update",
        "bank",
        "paypal",
        "wallet",
        "password",
        "confirm",
        "signin",
        "security",
        "billing"
    ];

    public LinkThreatFeatures Extract(LinkScanResult scanResult)
    {
        ArgumentNullException.ThrowIfNull(scanResult);

        var url = scanResult.Url ?? string.Empty;

        if(!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
           return new LinkThreatFeatures
           {
               UrlLength = url.Length,
               RiskScore = scanResult.RiskScore,
               StatusCode = scanResult.StatusCode
           }; 
        }

        var host = uri.Host;
        var path = uri.AbsolutePath;
        var query = uri.Query;

        return new LinkThreatFeatures
        {
            UrlLength = url.Length,
            HostLength = host.Length,
            PathLength = path.Length,
            QueryLength = query.Length,

            DotsCount = Count(url, '.'),
            HyphenCount = Count(url, '-'),
            DigitsCount = CountDigits(url),
            SpecialCharactersCount = CountSpecialCharacters(url),

            UsesHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase),
            HasIpAddressAsHost = IPAddress.TryParse(host, out _),
            HasAtSymbol = url.Contains('@'),

            SubdomainCount = CalculateSubdomainCount(host),
            SuspiciousKeywordCount = CountSuspiciousKeywords(url),

            StatusCode = scanResult.StatusCode ?? 0,
            RedirectCount = scanResult.RedirectChain?.Count ?? 0,
            RiskScore = scanResult.RiskScore,

            HasTitle = !string.IsNullOrWhiteSpace(scanResult.Title),
            TitleLength = scanResult.Title?.Length ?? 0,

            HasDescription = !string.IsNullOrWhiteSpace(scanResult.Description),
            DescriptionLength = scanResult.Description?.Length ?? 0,

            LinksCount = scanResult.LinksCount,
            ScriptsCount = scanResult.ScriptsCount,
            ImagesCount = scanResult.ImageCount,

            HasMixedContent = scanResult.MixedContent,

            HasContentSecurityPolicy = HasSecurityHeader(scanResult, "Content-Security-Policy"),
            HasStrictTransportSecurity = HasSecurityHeader(scanResult, "Strict-Transport-Security"),
            HasXFrameOptions = HasSecurityHeader(scanResult, "X-Frame-Options"),

            HasServerHeader = !string.IsNullOrWhiteSpace(scanResult.ServerHeader),
            HasXPoweredByHeader = !string.IsNullOrWhiteSpace(scanResult.XPoweredBy),

            HtmlBytes = scanResult.HtmlBytes ?? 0,
            LoadTimeMs = GetLoadTimeMs(scanResult),
            TimeToFirstByteMs = scanResult.TtfbMs ?? 0,

            CertificateDaysToExpiry = scanResult.CertDaysToExpiry ?? 0
        };
    }

    private static float Count(string value, char character)
    {
        return value.Count(c => c == character);
    }

    private static float CountDigits(string value)
    {
        return value.Count(char.IsDigit);
    }

    private static float CountSpecialCharacters(string value)
    {
        return value.Count(c => !char.IsLetterOrDigit(c));
    }

    private static float CountSuspiciousKeywords(string value)
    {
        return SuspiciousKeywords.Count(keyword =>
            value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static float CalculateSubdomainCount(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return 0;
        }

        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length <= 2)
        {
            return 0;
        }

        return parts.Length - 2;
    }

    private static float GetLoadTimeMs(LinkScanResult scanResult)
    {
        return scanResult.LoadTime.HasValue
            ? (float)scanResult.LoadTime.Value.TotalMilliseconds
            : 0;
    }

    private static bool HasSecurityHeader(LinkScanResult scanResult, string headerName)
    {
        if (scanResult.RawHeaders is not null &&
            scanResult.RawHeaders.ContainsKey(headerName))
        {
            return true;
        }

        if (scanResult.RawContentHeaders is not null &&
            scanResult.RawContentHeaders.ContainsKey(headerName))
        {
            return true;
        }

        return false;
    }
}