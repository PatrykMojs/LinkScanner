using FluentAssertions;
using LinkScanner.Domain.Entities;
using LinkScanner.Infrastructure.Scanning.Analyzers;

namespace LinkScanner.Tests.Infrastructure.Scanning.Analyzers;

public sealed class RiskScoreCalculatorTests
{
    private readonly RiskScoreCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldAdd40Points_WhenUrlDoesNotUseHttps()
    {
        var result = CreateSafeResult();

        var score = _calculator.Calculate("http://example.com", result);

        score.Should().Be(40);
    }

    [Fact]
    public void Calculate_ShouldAdd25Points_WhenStatusCodeIsClientOrServerError()
    {
        var result = CreateSafeResult();
        result.StatusCode = 404;

        var score = _calculator.Calculate("https://example.com", result);

        score.Should().Be(25);
    }

    [Fact]
    public void Calculate_ShouldAdd5Points_WhenCspHeaderIsMissing()
    {
        var result = CreateSafeResult();
        result.Headers.HasCSP = false;

        var score = _calculator.Calculate("https://example.com", result);

        score.Should().Be(5);
    }

    [Fact]
    public void Calculate_ShouldAdd5Points_WhenHstsHeaderIsMissingForHttpsUrl()
    {
        var result = CreateSafeResult();
        result.Headers.HasHSTS = false;

        var score = _calculator.Calculate("https://example.com", result);

        score.Should().Be(5);
    }

    [Fact]
    public void Calculate_ShouldNotAddHstsPenalty_WhenUrlUsesHttp()
    {
        var result = CreateSafeResult();
        result.Headers.HasHSTS = false;

        var score = _calculator.Calculate("http://example.com", result);

        score.Should().Be(40);
    }

    [Fact]
    public void Calculate_ShouldAdd5Points_WhenTitleIsEmpty()
    {
        var result = CreateSafeResult();
        result.Title = "";

        var score = _calculator.Calculate("https://example.com", result);

        score.Should().Be(5);
    }

    [Fact]
    public void Calculate_ShouldAdd10Points_WhenMixedContentExists()
    {
        var result = CreateSafeResult();
        result.MixedContent = true;

        var score = _calculator.Calculate("https://example.com", result);

        score.Should().Be(10);
    }

    [Fact]
    public void Calculate_ShouldAdd10Points_WhenCertificateExpiresSoon()
    {
        var result = CreateSafeResult();
        result.CertDaysToExpiry = 7;

        var score = _calculator.Calculate("https://example.com", result);

        score.Should().Be(10);
    }

    [Fact]
    public void Calculate_ShouldReturn95Points_ForHighlyRiskyHttpUrl()
    {
        var result = new LinkScanResult
        {
            StatusCode = 500,
            Title = "",
            MixedContent = true,
            CertDaysToExpiry = 1,
            Headers = new SecurityHeaders
            {
                HasCSP = false,
                HasHSTS = false
            }
        };

        var score = _calculator.Calculate("http://example.com", result);

        score.Should().Be(95);
    }

    private static LinkScanResult CreateSafeResult()
    {
        return new LinkScanResult
        {
            StatusCode = 200,
            Title = "Example page",
            MixedContent = false,
            CertDaysToExpiry = 365,
            Headers = new SecurityHeaders
            {
                HasCSP = true,
                HasHSTS = true,
                HasXFO = true,
                HasXCTO = true,
                HasReferrerPolicy = true,
                HasPermissionsPolicy = true
            }
        };
    }
}