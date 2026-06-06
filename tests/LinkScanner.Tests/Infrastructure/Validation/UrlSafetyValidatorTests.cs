using FluentAssertions;
using LinkScanner.Infrastructure.Validation;
using LinkScanner.Application.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace LinkScanner.Tests.Infrastructure.Validation;

public sealed class UrlSafetyValidatorTests
{
    private readonly Mock<IDnsResolver> _dnsResolverMock = new();
    private readonly UrlSafetyValidator _validator;

    public UrlSafetyValidatorTests()
    {
        var options = Options.Create(new LinkScannerOptions
        {
            HttpTimeoutSeconds = 8,
            MaxRedirects = 5,
            MaxHtmlBytes = 1_000_000,
            MaxUrlLength = 2048,
            AllowedPorts = [80, 443]
        });

        _validator = new UrlSafetyValidator(
            NullLogger<UrlSafetyValidator>.Instance,
            _dnsResolverMock.Object,
            options);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ValidateAsync_ShouldReturnFailure_WhenUrlIsEmpty(string? url)
    {
        var result = await _validator.ValidateAsync(url!);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Adres URL jest pusty.");

        _dnsResolverMock.Verify(
            x => x.GetHostAddressesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("example.com")]
    [InlineData("www.example.com")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenUrlIsInvalid(string url)
    {
        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Nieprawidłowy adres URL.");

        _dnsResolverMock.Verify(
            x => x.GetHostAddressesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("file:///C:/test.txt")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenSchemeIsNotHttpOrHttps(string url)
    {
        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Dozwolone są tylko adresy HTTP i HTTPS.");

        _dnsResolverMock.Verify(
            x => x.GetHostAddressesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("https://localhost")]
    [InlineData("http://LOCALHOST")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenHostIsLocalhost(string url)
    {
        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Nie można skanować localhost.");

        _dnsResolverMock.Verify(
            x => x.GetHostAddressesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://10.0.0.5")]
    [InlineData("http://172.16.0.1")]
    [InlineData("http://172.31.255.255")]
    [InlineData("http://192.168.1.10")]
    [InlineData("http://169.254.1.1")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenUrlPointsToPrivateOrLocalIpv4Address(string url)
    {
        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Adres prowadzi do sieci prywatnej lub lokalnej.");

        _dnsResolverMock.Verify(
            x => x.GetHostAddressesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("http://[::1]")]
    [InlineData("http://[fe80::1]")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenUrlPointsToPrivateOrLocalIpv6Address(string url)
    {
        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Adres prowadzi do sieci prywatnej lub lokalnej.");

        _dnsResolverMock.Verify(
            x => x.GetHostAddressesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenDnsResolvesToPublicIpAddress()
    {
        var url = "https://example.com";

        _dnsResolverMock
            .Setup(x => x.GetHostAddressesAsync("example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync([IPAddress.Parse("93.184.216.34")]);

        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        _dnsResolverMock.Verify(
            x => x.GetHostAddressesAsync("example.com", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.1.1")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenDnsResolvesToPrivateOrLocalIpAddress(string ip)
    {
        var url = "https://example.com";

        _dnsResolverMock
            .Setup(x => x.GetHostAddressesAsync("example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync([IPAddress.Parse(ip)]);

        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Adres prowadzi do sieci prywatnej lub lokalnej.");
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenDnsResolutionFails()
    {
        var url = "https://example.com";

        _dnsResolverMock
            .Setup(x => x.GetHostAddressesAsync("example.com", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Net.Sockets.SocketException());

        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Nie udało się rozwiązać adresu hosta.");
    }

    [Fact]
    public async Task ValidateAsync_ShouldNotCallDnsResolver_WhenHostIsDirectIpAddress()
    {
        var url = "https://93.184.216.34";

        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeTrue();

        _dnsResolverMock.Verify(
            x => x.GetHostAddressesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_ShouldNotCallDnsResolver_WhenUrlIsInvalid()
    {
        var url = "not-a-url";

        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();

        _dnsResolverMock.Verify(
            x => x.GetHostAddressesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("https://user:pass@example.com")]
    [InlineData("http://admin:secret@example.com")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenUrlContainsUserInfo(string url)
    {
        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Adres URL nie może zawierać danych logowania.");
    }
}