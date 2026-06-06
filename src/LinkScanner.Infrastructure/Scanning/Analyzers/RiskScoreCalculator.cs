using LinkScanner.Domain.Entities;

namespace LinkScanner.Infrastructure.Scanning.Analyzers;

public sealed class RiskScoreCalculator
{
    public int Calculate(string url, LinkScanResult result)
    {
        var score = 0;

        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            score += 40;

        if ((result.StatusCode ?? 200) >= 400)
            score += 25;

        if (result.Headers is { HasCSP: false })
            score += 5;

        if (result.Headers is { HasHSTS: false } && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            score += 5;

        if (string.IsNullOrWhiteSpace(result.Title))
            score += 5;

        if (result.MixedContent)
            score += 10;

        if ((result.CertDaysToExpiry ?? 365) < 14)
            score += 10;

        return Math.Min(100, score);
    }
}