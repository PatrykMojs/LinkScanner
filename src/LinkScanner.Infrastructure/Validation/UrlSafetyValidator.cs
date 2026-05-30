using System.Net;
using System.Net.Sockets;
using LinkScanner.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LinkScanner.Infrastructure.Validation;

public sealed class UrlSafetyValidator : IUrlSafetyValidator
{
    private readonly ILogger<UrlSafetyValidator> _logger;
    private readonly IDnsResolver _dnsResolver;

    public UrlSafetyValidator(ILogger<UrlSafetyValidator> logger, IDnsResolver dnsResolver)
    {
        _logger = logger;
        _dnsResolver = dnsResolver;
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

        if(IPAddress.TryParse(uri.Host, out var directIp))
        {
            if(IsPrivateOrLocalAddress(directIp))
            {
                return UrlSafetyValidationResult.Failure("Adres prowadzi do sieci prywatnej lub lokalnej.");
            }

            return UrlSafetyValidationResult.Success();
        }

        IPAddress[] addresses;

        try
        {
            addresses = await _dnsResolver.GetHostAddressesAsync(uri.Host, cancellationToken);
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex, "DNS resolution failed for host: {Host}", uri.Host);
            return UrlSafetyValidationResult.Failure("Nie udało się rozwiązać adresu hosta.");
        }

        if (addresses.Any(IsPrivateOrLocalAddress))
        {
            return UrlSafetyValidationResult.Failure("Adres prowadzi do sieci prywatnej lub lokalnej.");
        }


        return UrlSafetyValidationResult.Success();
    }

    private static bool IsPrivateOrLocalAddress(IPAddress ipAddress)
    {
        if (IPAddress.IsLoopback(ipAddress))
            return true;

        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();

            return bytes[0] == 10
                   || bytes[0] == 127
                   || bytes[0] == 169 && bytes[1] == 254
                   || bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31
                   || bytes[0] == 192 && bytes[1] == 168;
        }

        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return ipAddress.IsIPv6LinkLocal
                   || ipAddress.IsIPv6SiteLocal
                   || ipAddress.Equals(IPAddress.IPv6Loopback);
        }

        return false;
    }
}