using System.Net;
using System.Net.Sockets;
using LinkScanner.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LinkScanner.Infrastructure.Validation;

public sealed class UrlSafetyValidator : IUrlSafetyValidator
{
    private readonly ILogger<UrlSafetyValidator> _logger;

    public UrlSafetyValidator(ILogger<UrlSafetyValidator> logger)
    {
        _logger = logger;
    }

    public async Task<UrlSafetyValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return UrlSafetyValidationResult.Failure("Adres URL jest pusty.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return UrlSafetyValidationResult.Failure("Nieprawidłowy adres URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return UrlSafetyValidationResult.Failure("Dozwolone są tylko adresy HTTP i HTTPS.");
        }

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Blocked localhost URL: {Url}", url);
            return UrlSafetyValidationResult.Failure("Nie można skanować localhost.");
        }

        IPAddress[] addresses;

        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex, "DNS resolution failed for host: {Host}", uri.Host);
            return UrlSafetyValidationResult.Failure("Nie udało się rozwiązać adresu hosta.");
        }

        foreach (var ip in addresses)
        {
            if (IsPrivateOrLocalIp(ip))
            {
                _logger.LogWarning("Blocked private or local IP address. Url: {Url}, IP: {IpAddress}", url, ip);
                return UrlSafetyValidationResult.Failure("Adres prowadzi do sieci prywatnej lub lokalnej.");
            }
        }

        return UrlSafetyValidationResult.Success();
    }

    private static bool IsPrivateOrLocalIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();

            return
                bytes[0] == 10 ||
                bytes[0] == 127 ||
                bytes[0] == 0 ||
                bytes[0] == 169 && bytes[1] == 254 ||
                bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ||
                bytes[0] == 192 && bytes[1] == 168;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return ip.IsIPv6LinkLocal ||
                   ip.IsIPv6SiteLocal ||
                   ip.IsIPv6Multicast ||
                   ip.Equals(IPAddress.IPv6Loopback);
        }

        return true;
    }
}