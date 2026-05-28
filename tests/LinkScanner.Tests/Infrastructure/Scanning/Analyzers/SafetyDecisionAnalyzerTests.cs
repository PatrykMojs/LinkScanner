using FluentAssertions;
using LinkScanner.Domain.Entities;
using LinkScanner.Infrastructure.Scanning.Analyzers;

namespace LinkScanner.Tests.Infrastructure.Scanning.Analyzers;

public sealed class SafetyDecisionAnalyzerTests
{
    private readonly SafetyDecisionAnalyzer _analyzer = new();

    [Fact]
    public void IsSafe_ShouldReturnTrue_WhenUrlAndScanResultAreSafe()
    {
        var result = CreateSafeResult();

        var isSafe = _analyzer.IsSafe("https://example.com", result);

        isSafe.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://example.com/phishing")]
    [InlineData("https://example.com/malware")]
    [InlineData("https://example.com/scam")]
    [InlineData("https://example.com/fake-login")]
    [InlineData("https://example.com/verify-account")]
    [InlineData("https://example.com/password-reset")]
    [InlineData("https://example.com/free-gift")]
    public void IsSafe_ShouldReturnFalse_WhenUrlContainsSuspiciousKeyword(string url)
    {
        var result = CreateSafeResult();

        var isSafe = _analyzer.IsSafe(url, result);

        isSafe.Should().BeFalse();
    }

    [Fact]
    public void IsSafe_ShouldReturnFalse_WhenRiskScoreIs70()
    {
        var result = CreateSafeResult();
        result.RiskScore = 70;

        var isSafe = _analyzer.IsSafe("https://example.com", result);

        isSafe.Should().BeFalse();
    }

    [Fact]
    public void IsSafe_ShouldReturnFalse_WhenRiskScoreIsGreaterThan70()
    {
        var result = CreateSafeResult();
        result.RiskScore = 85;

        var isSafe = _analyzer.IsSafe("https://example.com", result);

        isSafe.Should().BeFalse();
    }

    [Fact]
    public void IsSafe_ShouldReturnTrue_WhenRiskScoreIsBelow70()
    {
        var result = CreateSafeResult();
        result.RiskScore = 69;

        var isSafe = _analyzer.IsSafe("https://example.com", result);

        isSafe.Should().BeTrue();
    }

    [Fact]
    public void IsSafe_ShouldReturnFalse_WhenStatusCodeIs500()
    {
        var result = CreateSafeResult();
        result.StatusCode = 500;

        var isSafe = _analyzer.IsSafe("https://example.com", result);

        isSafe.Should().BeFalse();
    }

    [Fact]
    public void IsSafe_ShouldReturnFalse_WhenStatusCodeIsGreaterThan500()
    {
        var result = CreateSafeResult();
        result.StatusCode = 503;

        var isSafe = _analyzer.IsSafe("https://example.com", result);

        isSafe.Should().BeFalse();
    }

    [Fact]
    public void IsSafe_ShouldReturnTrue_WhenStatusCodeIs499()
    {
        var result = CreateSafeResult();
        result.StatusCode = 499;

        var isSafe = _analyzer.IsSafe("https://example.com", result);

        isSafe.Should().BeTrue();
    }

    [Fact]
    public void IsSafe_ShouldReturnFalse_WhenCertificateIsExpired()
    {
        var result = CreateSafeResult();
        result.CertDaysToExpiry = -1;

        var isSafe = _analyzer.IsSafe("https://example.com", result);

        isSafe.Should().BeFalse();
    }

    [Fact]
    public void IsSafe_ShouldReturnTrue_WhenCertificateExpiryIsUnknown()
    {
        var result = CreateSafeResult();
        result.CertDaysToExpiry = null;

        var isSafe = _analyzer.IsSafe("https://example.com", result);

        isSafe.Should().BeTrue();
    }

    [Fact]
    public void IsSafe_ShouldReturnFalse_WhenSuspiciousKeywordHasDifferentCasing()
    {
        var result = CreateSafeResult();

        var isSafe = _analyzer.IsSafe("https://example.com/PhIsHiNg", result);

        isSafe.Should().BeFalse();
    }

    private static LinkScanResult CreateSafeResult()
    {
        return new LinkScanResult
        {
            RiskScore = 0,
            StatusCode = 200,
            CertDaysToExpiry = 365
        };
    }
}