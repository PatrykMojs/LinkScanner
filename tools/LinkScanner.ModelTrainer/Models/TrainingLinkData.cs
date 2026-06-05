using Microsoft.ML.Data;

namespace LinkScanner.ModelTrainer.Models;

public sealed class TrainingLinkData
{
    [LoadColumn(0)]
    public bool Label { get; set; }

    [LoadColumn(1)]
    public float UrlLength { get; set; }

    [LoadColumn(2)]
    public float DotsCount { get; set; }

    [LoadColumn(3)]
    public float HyphenCount { get; set; }

    [LoadColumn(4)]
    public float DigitsCount { get; set; }

    [LoadColumn(5)]
    public float SpecialCharactersCount { get; set; }

    [LoadColumn(6)]
    public float UsesHttps { get; set; }

    [LoadColumn(7)]
    public float HasIpAddressAsHost { get; set; }

    [LoadColumn(8)]
    public float HasAtSymbol { get; set; }

    [LoadColumn(9)]
    public float SubdomainCount { get; set; }

    [LoadColumn(10)]
    public float SuspiciousKeywordCount { get; set; }
}