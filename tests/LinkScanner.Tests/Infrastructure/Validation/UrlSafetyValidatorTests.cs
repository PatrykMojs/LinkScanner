using FluentAssertions;
using LinkScanner.Infrastructure.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace LinkScanner.Tests.Infrastructure.Validation;

public sealed class UrlSafetyValidatorTests
{
    private readonly UrlSafetyValidator _validator = new(NullLogger<UrlSafetyValidator>.Instance);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ValidateAsync_ShouldReturnFailure_WhenUrlIsEmpty(string? url)
    {
        var result = await _validator.ValidateAsync(url!);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Adres URL jest pusty.");
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
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("file:///C:/test.txt")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenSchemeIsNotHttpOrHttps(string url)
    {
        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Dozwolone są tylko adresy HTTP i HTTPS.");
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
    }

    [Theory]
    [InlineData("http://[::1]")]
    [InlineData("http://[fe80::1]")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenUrlPointsToPrivateOrLocalIpv6Address(string url)
    {
        var result = await _validator.ValidateAsync(url);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Adres prowadzi do sieci prywatnej lub lokalnej.");
    }
}