using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace LinkScanner.Infrastructure.Scanning.Analyzers;

public sealed class TlsCertificateAnalyzer
{
    public async Task<TlsCertificateInfo?> AnalyzeAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri(url);

            if (uri.Scheme != Uri.UriSchemeHttps)
                return null;

            using var client = new TcpClient();

            await client.ConnectAsync(uri.Host, 443, cancellationToken);

            await using var stream = client.GetStream();

            using var ssl = new SslStream(
                stream,
                leaveInnerStreamOpen: false,
                userCertificateValidationCallback: (_, cert, _, _) => cert is not null);

            await ssl.AuthenticateAsClientAsync(uri.Host);

            var cert = new X509Certificate2(ssl.RemoteCertificate!);

            return new TlsCertificateInfo
            {
                Protocol = ssl.SslProtocol.ToString(),
                Subject = cert.Subject,
                Issuer = cert.Issuer,
                NotAfter = cert.NotAfter
            };
        }
        catch
        {
            return null;
        }
    }
}

public sealed class TlsCertificateInfo
{
    public string Protocol { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public DateTimeOffset NotAfter { get; init; }
}