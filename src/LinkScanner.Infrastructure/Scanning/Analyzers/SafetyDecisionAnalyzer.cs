using LinkScanner.Domain.Entities;

namespace LinkScanner.Infrastructure.Scanning.Analyzers;

public sealed class SafetyDecisionAnalyzer
{
    private static readonly string[] SuspiciousKeywords =
    [
        "phishing",
        "malware",
        "scam",
        "fake-login",
        "verify-account",
        "password-reset",
        "free-gift"
    ];

    public bool IsSafe(string url, LinkScanResult result)
    {
        if (ContainsSuspiciousKeyword(url))
        {
            return false;
        }

        if (result.RiskScore >= 70)
        {
            return false;
        }

        if (result.StatusCode >= 500)
        {
            return false;
        }

        if (result.CertDaysToExpiry is not null && result.CertDaysToExpiry < 0)
        {
            return false;
        }

        return true;
    }

    private static bool ContainsSuspiciousKeyword(string url)
    {
        return SuspiciousKeywords.Any(keyword =>
            url.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}