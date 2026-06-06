using System.Net;

namespace LinkScanner.Infrastructure.Scanning.Analyzers;

public sealed class HostIpResolver
{
    public async Task<List<string>> ResolveAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var host = new Uri(url).Host;
            var ips = await Dns.GetHostAddressesAsync(host, cancellationToken);

            return ips
                .Select(ip => ip.ToString())
                .Distinct()
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}