namespace LinkScanner.Application.Abstractions;

public sealed class UrlSafetyValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    private UrlSafetyValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static UrlSafetyValidationResult Success()
    {
        return new UrlSafetyValidationResult(true, null);
    }

    public static UrlSafetyValidationResult Failure(string errorMessage)
    {
        return new UrlSafetyValidationResult(false, errorMessage);
    }
}