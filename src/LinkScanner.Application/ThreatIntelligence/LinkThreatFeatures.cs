namespace LinkScanner.Application.ThreatIntelligence;

public sealed class LinkThreatFeatures
{
    public float UrlLength { get; set; }
    public float HostLength { get; set; }
    public float PathLength { get; set; }
    public float QueryLength { get; set; }
    public float DotsCount { get; set; }
    public float HyphenCount { get; set; }
    public float DigitsCount { get; set; }
    public float SpecialCharactersCount { get; set; }
    public bool UsesHttps { get; set; }
    public bool HasIpAddressAsHost { get; set; }
    public bool HasAtSymbol { get; set; }
    public float SubdomainCount { get; set; }
    public float SuspiciousKeywordCount { get; set; }
    public int? StatusCode { get; set; }
    public float RedirectCount { get; set; }
    public float RiskScore { get; set; }
    public bool HasTitle { get; set; }
    public float TitleLength { get; set; }
    public bool HasDescription { get; set; }
    public float DescriptionLength { get; set; }
    public float LinksCount { get; set; }
    public float ScriptsCount { get; set; }
    public float ImagesCount { get; set; }
    public bool HasMixedContent { get; set; }
    public bool HasContentSecurityPolicy { get; set; }
    public bool HasStrictTransportSecurity { get; set; }
    public bool HasXFrameOptions { get; set; }
    public bool HasServerHeader { get; set; }
    public bool HasXPoweredByHeader { get; set; }
    public float HtmlBytes { get; set; }
    public float LoadTimeMs { get; set; }
    public float TimeToFirstByteMs { get; set; }
    public float CertificateDaysToExpiry { get; set; }
}