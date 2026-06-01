using System.Net;
using System.Net.Sockets;
using LinkScanner.Application.Abstractions;
using LinkScanner.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkScanner.Infrastructure.Validation;

public sealed class UrlSafetyValidator : IUrlSafetyValidator
{
    private readonly ILogger<UrlSafetyValidator> _logger;
    private readonly IDnsResolver _dnsResolver;
    private readonly LinkScannerOptions _options;

    public UrlSafetyValidator(ILogger<UrlSafetyValidator> logger, IDnsResolver dnsResolver, IOptions<LinkScannerOptions> options)
    {
        _logger = logger;
        _dnsResolver = dnsResolver;
        _options = options.Value;
    }

    public async Task<UrlSafetyValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return UrlSafetyValidationResult.Failure("Adres URL jest pusty.");
        }

        if (url.Length > _options.MaxUrlLength)
        {
            return UrlSafetyValidationResult.Failure("Adres URL jest zbyt długi.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return UrlSafetyValidationResult.Failure("Nieprawidłowy adres URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return UrlSafetyValidationResult.Failure("Dozwolone są tylko adresy HTTP i HTTPS.");
        }

        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            return UrlSafetyValidationResult.Failure("Adres URL nie może zawierać danych logowania.");
        }

        if (!IsAllowedPort(uri))
        {
            return UrlSafetyValidationResult.Failure("Port adresu URL nie jest dozwolony.");
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            return UrlSafetyValidationResult.Failure("Adres URL nie zawiera poprawnego hosta.");
        }

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Blocked localhost URL: {Url}", url);
            return UrlSafetyValidationResult.Failure("Nie można skanować localhost.");
        }

        if (IPAddress.TryParse(uri.Host, out var directIp))
        {
            if (IsPrivateOrLocalAddress(directIp))
            {
                _logger.LogWarning("Blocked private or local IP address: {IpAddress}", directIp);
                return UrlSafetyValidationResult.Failure("Adres prowadzi do sieci prywatnej lub lokalnej.");
            }

            return UrlSafetyValidationResult.Success();
        }

        IPAddress[] addresses;

        try
        {
            addresses = await _dnsResolver.GetHostAddressesAsync(uri.Host, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DNS resolution failed for host: {Host}", uri.Host);
            return UrlSafetyValidationResult.Failure("Nie udało się rozwiązać adresu hosta.");
        }

        if (addresses.Length == 0)
        {
            return UrlSafetyValidationResult.Failure("Host nie posiada przypisanego adresu IP.");
        }

        if (addresses.Any(IsPrivateOrLocalAddress))
        {
            _logger.LogWarning(
                "Blocked host resolving to private/local address. Host: {Host}, Addresses: {Addresses}",
                uri.Host,
                string.Join(", ", addresses.Select(x => x.ToString())));

            return UrlSafetyValidationResult.Failure("Adres prowadzi do sieci prywatnej lub lokalnej.");
        }

        return UrlSafetyValidationResult.Success();
    }

    private bool IsAllowedPort(Uri uri)
    {
        if(uri.IsDefaultPort)
        {
            return true;
        }
        
        return _options.AllowedPorts.Contains(uri.Port);
    }

    private static bool IsPrivateOrLocalAddress(IPAddress ipAddress)
    {
        if (IPAddress.IsLoopback(ipAddress))
        {
            return true;
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();

            return
                bytes[0] == 0 ||
                bytes[0] == 10 ||
                bytes[0] == 127 ||
                bytes[0] == 169 && bytes[1] == 254 ||
                bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ||
                bytes[0] == 192 && bytes[1] == 168;
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return
                ipAddress.IsIPv6LinkLocal ||
                ipAddress.IsIPv6SiteLocal ||
                ipAddress.Equals(IPAddress.IPv6Loopback);
        }

        return false;
    }
}