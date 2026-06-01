using FluentAssertions;
using LinkScanner.Application.Abstractions;
using LinkScanner.Application.Options;
using LinkScanner.Infrastructure.Scanning.Analyzers;
using LinkScanner.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace LinkScanner.Tests.Infrastructure.Scanning.Analyzers;

public sealed class RedirectAnalyzerTests
{
    private readonly Mock<IUrlSafetyValidator> _urlSafetyValidatorMock = new();
    private readonly Mock<IRedirectHttpClient> _redirectHttpClientMock = new();
    private readonly RedirectAnalyzer _analyzer;

    public RedirectAnalyzerTests()
    {
        _analyzer = CreateAnalyzer();
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnEmptyList_WhenResponseHasNoLocationHeader()
    {
        var url = "https://example.com";

        _redirectHttpClientMock
            .Setup(x => x.SendAsync(url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedirectHttpResult(200, null));

        var result = await _analyzer.AnalyzeAsync(url);

        result.Should().BeEmpty();

        _urlSafetyValidatorMock.Verify(
            x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldAddRedirect_WhenLocationIsAbsoluteAndValid()
    {
        var url = "https://example.com";
        var nextUrl = "https://example.com/login";

        _redirectHttpClientMock
            .Setup(x => x.SendAsync(url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedirectHttpResult(301, new Uri(nextUrl)));

        _redirectHttpClientMock
            .Setup(x => x.SendAsync(nextUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedirectHttpResult(200, null));

        _urlSafetyValidatorMock
            .Setup(x => x.ValidateAsync(nextUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UrlSafetyValidationResult.Success());

        var result = await _analyzer.AnalyzeAsync(url);

        result.Should().ContainSingle();
        result[0].Should().Be("301 -> https://example.com/login");

        _urlSafetyValidatorMock.Verify(
            x => x.ValidateAsync(nextUrl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldResolveRelativeRedirectUrl()
    {
        var url = "https://example.com/start";
        var resolvedUrl = "https://example.com/login";

        _redirectHttpClientMock
            .Setup(x => x.SendAsync(url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedirectHttpResult(302, new Uri("/login", UriKind.Relative)));

        _redirectHttpClientMock
            .Setup(x => x.SendAsync(resolvedUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedirectHttpResult(200, null));

        _urlSafetyValidatorMock
            .Setup(x => x.ValidateAsync(resolvedUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UrlSafetyValidationResult.Success());

        var result = await _analyzer.AnalyzeAsync(url);

        result.Should().ContainSingle();
        result[0].Should().Be("302 -> https://example.com/login");

        _urlSafetyValidatorMock.Verify(
            x => x.ValidateAsync(resolvedUrl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldStopAndMarkRedirectAsBlocked_WhenNextUrlIsInvalid()
    {
        var url = "https://example.com";
        var blockedUrl = "http://localhost/admin";
        var errorMessage = "Nie można skanować localhost.";

        _redirectHttpClientMock
            .Setup(x => x.SendAsync(url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedirectHttpResult(302, new Uri(blockedUrl)));

        _urlSafetyValidatorMock
            .Setup(x => x.ValidateAsync(blockedUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UrlSafetyValidationResult.Failure(errorMessage));

        var result = await _analyzer.AnalyzeAsync(url);

        result.Should().ContainSingle();
        result[0].Should().Be("302 -> BLOCKED: Nie można skanować localhost.");

        _redirectHttpClientMock.Verify(
            x => x.SendAsync(url, It.IsAny<CancellationToken>()),
            Times.Once);

        _redirectHttpClientMock.Verify(
            x => x.SendAsync(blockedUrl, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldStopAfterConfiguredMaxRedirects()
    {
        var analyzer = CreateAnalyzer(maxRedirects: 1);

        var firstUrl = "https://example.com/1";
        var secondUrl = "https://example.com/2";
        var thirdUrl = "https://example.com/3";

        _redirectHttpClientMock
            .Setup(x => x.SendAsync(firstUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedirectHttpResult(301, new Uri(secondUrl)));

        _redirectHttpClientMock
            .Setup(x => x.SendAsync(secondUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedirectHttpResult(302, new Uri(thirdUrl)));

        _urlSafetyValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UrlSafetyValidationResult.Success());

        var result = await analyzer.AnalyzeAsync(firstUrl);

        result.Should().ContainSingle();
        result[0].Should().Be("301 -> https://example.com/2");

        _redirectHttpClientMock.Verify(
            x => x.SendAsync(firstUrl, It.IsAny<CancellationToken>()),
            Times.Once);

        _redirectHttpClientMock.Verify(
            x => x.SendAsync(secondUrl, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private RedirectAnalyzer CreateAnalyzer(int maxRedirects = 5)
    {
        var options = Options.Create(new LinkScannerOptions
        {
            HttpTimeoutSeconds = 8,
            MaxRedirects = maxRedirects,
            MaxHtmlBytes = 1_000_000,
            MaxUrlLength = 2048,
            AllowedPorts = [80, 443]
        });

        return new RedirectAnalyzer(
            NullLogger<RedirectAnalyzer>.Instance,
            _urlSafetyValidatorMock.Object,
            _redirectHttpClientMock.Object,
            options);
    }
}