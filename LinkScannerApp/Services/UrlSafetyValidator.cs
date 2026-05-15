using System.Net;
using System.Net.Sockets;

namespace LinkScannerApp.Services;

public static class UrlSafetyValidator
{
    public static async Task<(bool IsValid, string? Error)> ValidateAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, "Adres URL jest pusty.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (false, "Nieprawidłowy adres URL.");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return (false, "Dozwolone są tylko adresy HTTP i HTTPS.");

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return (false, "Nie można skanować localhost.");

        IPAddress[] addresses;

        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host);
        }
        catch
        {
            return (false, "Nie udało się rozwiązać adresu hosta.");
        }

        foreach (var ip in addresses)
        {
            if (IsPrivateOrLocalIp(ip))
                return (false, "Adres prowadzi do sieci prywatnej lub lokalnej.");
        }

        return (true, null);
    }

    private static bool IsPrivateOrLocalIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

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