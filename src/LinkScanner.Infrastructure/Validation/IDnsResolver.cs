using System.Net;

namespace LinkScanner.Infrastructure.Validation;

public interface IDnsResolver
{
    Task<IPAddress[]> GetHostAddressesAsync(string host, CancellationToken cancellationToken = default);
}