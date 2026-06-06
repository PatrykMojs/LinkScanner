using System.Net;

namespace LinkScanner.Infrastructure.Validation;

public sealed class SystemDnsResolver : IDnsResolver
{
    public Task<IPAddress[]> GetHostAddressesAsync(string host, CancellationToken cancellationToken = default)
    {
        return Dns.GetHostAddressesAsync(host, cancellationToken);
    }
}