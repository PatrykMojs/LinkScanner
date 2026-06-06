using FluentAssertions;
using LinkScanner.Infrastructure.Scanning.Analyzers;

namespace LinkScanner.Tests.Infrastructure.Scanning.Analyzers;

public sealed class SecurityHeadersAnalyzerTests
{
    private readonly SecurityHeadersAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_ShouldReturnTrueForAllHeaders_WhenAllSecurityHeadersExist()
    {
        using var response = new HttpResponseMessage();

        response.Headers.Add("Content-Security-Policy", "default-src 'self'");
        response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
        response.Headers.Add("X-Frame-Options", "DENY");
        response.Headers.Add("X-Content-Type-Options", "nosniff");
        response.Headers.Add("Referrer-Policy", "no-referrer");
        response.Headers.Add("Permissions-Policy", "geolocation=()");

        var result = _analyzer.Analyze(response);

        result.HasCSP.Should().BeTrue();
        result.HasHSTS.Should().BeTrue();
        result.HasXFO.Should().BeTrue();
        result.HasXCTO.Should().BeTrue();
        result.HasReferrerPolicy.Should().BeTrue();
        result.HasPermissionsPolicy.Should().BeTrue();
    }

    [Fact]
    public void Analyze_ShouldReturnFalseForAllHeaders_WhenNoSecurityHeadersExist()
    {
        using var response = new HttpResponseMessage();

        var result = _analyzer.Analyze(response);

        result.HasCSP.Should().BeFalse();
        result.HasHSTS.Should().BeFalse();
        result.HasXFO.Should().BeFalse();
        result.HasXCTO.Should().BeFalse();
        result.HasReferrerPolicy.Should().BeFalse();
        result.HasPermissionsPolicy.Should().BeFalse();
    }

    [Fact]
    public void Analyze_ShouldDetectContentSecurityPolicyHeader()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("Content-Security-Policy", "default-src 'self'");

        var result = _analyzer.Analyze(response);

        result.HasCSP.Should().BeTrue();
        result.HasHSTS.Should().BeFalse();
        result.HasXFO.Should().BeFalse();
        result.HasXCTO.Should().BeFalse();
        result.HasReferrerPolicy.Should().BeFalse();
        result.HasPermissionsPolicy.Should().BeFalse();
    }

    [Fact]
    public void Analyze_ShouldDetectStrictTransportSecurityHeader()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("Strict-Transport-Security", "max-age=31536000");

        var result = _analyzer.Analyze(response);

        result.HasHSTS.Should().BeTrue();
    }

    [Fact]
    public void Analyze_ShouldDetectXFrameOptionsHeader()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("X-Frame-Options", "DENY");

        var result = _analyzer.Analyze(response);

        result.HasXFO.Should().BeTrue();
    }

    [Fact]
    public void Analyze_ShouldDetectXContentTypeOptionsHeader()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("X-Content-Type-Options", "nosniff");

        var result = _analyzer.Analyze(response);

        result.HasXCTO.Should().BeTrue();
    }

    [Fact]
    public void Analyze_ShouldDetectReferrerPolicyHeader()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("Referrer-Policy", "no-referrer");

        var result = _analyzer.Analyze(response);

        result.HasReferrerPolicy.Should().BeTrue();
    }

    [Fact]
    public void Analyze_ShouldDetectPermissionsPolicyHeader()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("Permissions-Policy", "geolocation=()");

        var result = _analyzer.Analyze(response);

        result.HasPermissionsPolicy.Should().BeTrue();
    }

    [Fact]
    public void Analyze_ShouldDetectHeader_WhenHeaderExistsInContentHeaders()
    {
        using var response = new HttpResponseMessage
        {
            Content = new StringContent("test")
        };

        response.Content.Headers.Add("Content-Security-Policy", "default-src 'self'");

        var result = _analyzer.Analyze(response);

        result.HasCSP.Should().BeTrue();
    }
}