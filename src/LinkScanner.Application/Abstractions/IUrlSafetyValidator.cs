namespace LinkScanner.Application.Abstractions;

public interface IUrlSafetyValidator
{
    Task<UrlSafetyValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default);
}