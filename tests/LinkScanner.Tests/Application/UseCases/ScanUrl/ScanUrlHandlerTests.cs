using FluentAssertions;
using LinkScanner.Application.Abstractions;
using LinkScanner.Application.UseCases.ScanUrl;
using LinkScanner.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LinkScanner.Tests.Application.UseCases.ScanUrl;

public sealed class ScanUrlHandlerTests
{
    private readonly Mock<IUrlSafetyValidator> _validatorMock = new();
    private readonly Mock<ILinkScanner> _linkScannerMock = new();
    private readonly ScanUrlHandler _handler;

    public ScanUrlHandlerTests()
    {
        _handler = new ScanUrlHandler(NullLogger<ScanUrlHandler>.Instance, _validatorMock.Object, _linkScannerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnInvalidResult_WhenUrlValidationFails()
    {
        var url = "not-a-url";

        _validatorMock
            .Setup(x => x.ValidateAsync(url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UrlSafetyValidationResult.Failure("Nieprawidłowy adres URL."));

        var result = await _handler.HandleAsync(new ScanUrlCommand(url));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Nieprawidłowy adres URL.");
        result.Result.Should().BeNull();

        _linkScannerMock.Verify(
            x => x.ScanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallLinkScanner_WhenUrlValidationSucceeds()
    {
        var url = "https://example.com";

        var scanResult = new LinkScanResult
        {
            Url = url,
            IsSafe = true,
            StatusCode = 200,
            RiskScore = 10,
            Title = "Example page"
        };

        _validatorMock
            .Setup(x => x.ValidateAsync(url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UrlSafetyValidationResult.Success());

        _linkScannerMock
            .Setup(x => x.ScanAsync(url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanResult);

        var result = await _handler.HandleAsync(new ScanUrlCommand(url));

        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Result.Should().BeSameAs(scanResult);

        _linkScannerMock.Verify(
            x => x.ScanAsync(url, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken_ToValidatorAndScanner()
    {
        var url = "https://example.com";
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var scanResult = new LinkScanResult
        {
            Url = url,
            IsSafe = true,
            StatusCode = 200
        };

        _validatorMock
            .Setup(x => x.ValidateAsync(url, cancellationToken))
            .ReturnsAsync(UrlSafetyValidationResult.Success());

        _linkScannerMock
            .Setup(x => x.ScanAsync(url, cancellationToken))
            .ReturnsAsync(scanResult);

        var result = await _handler.HandleAsync(new ScanUrlCommand(url), cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Result.Should().BeSameAs(scanResult);

        _validatorMock.Verify(
            x => x.ValidateAsync(url, cancellationToken),
            Times.Once);

        _linkScannerMock.Verify(
            x => x.ScanAsync(url, cancellationToken),
            Times.Once);
    }
}