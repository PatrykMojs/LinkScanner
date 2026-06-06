using LinkScanner.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LinkScanner.Application.UseCases.ScanUrl;

public sealed class ScanUrlHandler
{
    private readonly ILogger<ScanUrlHandler> _logger;
    private readonly IUrlSafetyValidator _urlSafetyValidator;
    private readonly ILinkScanner _linkScanner;

    public ScanUrlHandler(ILogger<ScanUrlHandler> logger, IUrlSafetyValidator urlSafetyValidator, ILinkScanner linkScanner)
    {
        _logger = logger;
        _urlSafetyValidator = urlSafetyValidator;
        _linkScanner = linkScanner;
    }

    public async Task<ScanUrlResponse> HandleAsync(ScanUrlCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Url))
            return ScanUrlResponse.Failure("URL address cannot be empty.");

        var validationResult = await _urlSafetyValidator.ValidateAsync(command.Url, cancellationToken);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("URL validation failed for URL: {Url}. Reason: {Reason}", command.Url, validationResult.ErrorMessage);

            return ScanUrlResponse.Failure(validationResult.ErrorMessage ?? "URL address is not valid.");
        }

        var scanResult = await _linkScanner.ScanAsync(command.Url, cancellationToken);
        _logger.LogInformation("Scan URL use case finished for URL: {Url}", command.Url);

        return ScanUrlResponse.Success(scanResult);
    }
}