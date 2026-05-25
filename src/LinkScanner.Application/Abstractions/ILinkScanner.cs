using LinkScanner.Domain.Entities;

namespace LinkScanner.Application.Abstractions;

public interface ILinkScanner
{
    Task<LinkScanResult> ScanAsync(string url, CancellationToken cancellationToken = default);
}