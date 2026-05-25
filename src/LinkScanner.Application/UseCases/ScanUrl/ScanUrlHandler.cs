using LinkScanner.Application.Abstractions;

namespace LinkScanner.Application.UseCases.ScanUrl;

public sealed class ScanUrlHandler
{
    private readonly IUrlSafetyValidator _urlSafetyValidator;
    private readonly ILinkScanner _linkScanner;

    public ScanUrlHandler(IUrlSafetyValidator urlSafetyValidator, ILinkScanner linkScanner)
    {
        _urlSafetyValidator = urlSafetyValidator;
        _linkScanner = linkScanner;
    }

    public async Task<ScanUrlResponse> HandleAsync(ScanUrlCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Url))
            return ScanUrlResponse.Failure("URL address cannot be empty.");

        var validationResult = await _urlSafetyValidator.ValidateAsync(command.Url, cancellationToken);

        if (!validationResult.IsValid)
            return ScanUrlResponse.Failure(validationResult.ErrorMessage ?? "URL address is not valid.");

        var scanResult = await _linkScanner.ScanAsync(command.Url, cancellationToken);

        return ScanUrlResponse.Success(scanResult);
    }
}