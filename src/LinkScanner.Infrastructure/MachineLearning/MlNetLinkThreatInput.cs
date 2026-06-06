namespace LinkScanner.Infrastructure.MachineLearning;

public sealed class MlNetLinkThreatInput
{
    public float UrlLength { get; set; }
    public float DotsCount { get; set; }
    public float HyphenCount { get; set; }
    public float DigitsCount { get; set; }
    public float SpecialCharactersCount { get; set; }
    public float UsesHttps { get; set; }
    public float HasIpAddressAsHost { get; set; }
    public float HasAtSymbol { get; set; }
    public float SubdomainCount { get; set; }
    public float SuspiciousKeywordCount { get; set; }
}