namespace LinkScanner.Application.Abstractions.Scanning;

public interface IScanConcurrencyLimiter
{
    Task<IDisposable?> TryAcquireAsync(CancellationToken cancellationToken = default);
}