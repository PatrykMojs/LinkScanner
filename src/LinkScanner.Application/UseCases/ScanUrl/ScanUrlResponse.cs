using LinkScanner.Domain.Entities;

namespace LinkScanner.Application.UseCases.ScanUrl;

public sealed class ScanUrlResponse
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public LinkScanResult? Result { get; }

    private ScanUrlResponse(bool isSuccess, string? errorMessage, LinkScanResult? result)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Result = result;
    }

    public static ScanUrlResponse Success(LinkScanResult result)
    {
        return new ScanUrlResponse(true, null, result);
    }

    public static ScanUrlResponse Failure(string errorMessage)
    {
        return new ScanUrlResponse(false, errorMessage, null);
    }
}