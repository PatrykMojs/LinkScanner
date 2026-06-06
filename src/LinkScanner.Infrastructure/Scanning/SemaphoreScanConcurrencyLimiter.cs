using LinkScanner.Application.Abstractions.Scanning;
using LinkScanner.Application.Options;
using Microsoft.Extensions.Options;

namespace LinkScanner.Infrastructure.Scanning;

public sealed class SemaphoreScanConcurrencyLimiter : IScanConcurrencyLimiter
{
    private readonly SemaphoreSlim _semaphore;

    public SemaphoreScanConcurrencyLimiter(IOptions<LinkScannerOptions> options)
    {
        var maxConcurrentScans = Math.Max(1, options.Value.MaxConcurrentScans);

        _semaphore = new SemaphoreSlim(maxConcurrentScans, maxConcurrentScans);
    }

    public async Task<IDisposable?> TryAcquireAsync(CancellationToken cancellationToken = default)
    {
        var acquired = await _semaphore.WaitAsync(0, cancellationToken);

        return acquired
            ? new SemaphoreLease(_semaphore)
            : null;
    }

    private sealed class SemaphoreLease : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public SemaphoreLease(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if(_disposed)
                return;

            _semaphore.Release();
            _disposed = true;
        }
    }
}