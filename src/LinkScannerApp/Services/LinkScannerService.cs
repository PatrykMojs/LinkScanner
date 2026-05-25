using HtmlAgilityPack;
using System.Diagnostics;
using LinkScannerApp.Models;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace LinkScannerApp.Services
{
    public class LinkScannerService
    {
        private readonly HttpClient httpClient;

        public LinkScannerService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            this.httpClient.Timeout = TimeSpan.FromSeconds(8);
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LinkScannerApp/1.0");
        }

        public async Task<LinkScanResult> ScanAsync(string url)
        {
            var validation = await UrlSafetyValidator.ValidateAsync(url);

            if (!validation.IsValid)
            {
                return new LinkScanResult
                {
                    Url = url,
                    IsSafe = false,
                    RiskScore = 100,
                    StatusCode = null,
                    Title = validation.Error
                };
            }

            var result = new LinkScanResult { Url = url };

            try
            {
                result.HostIps = await ResolveIpsAsync(url);

                result.RedirectChain = await GetRedirectsAsync(url);

                var ttfb = new Stopwatch();
                string html;
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    ttfb.Start();
                    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    result.RawHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
                    result.RawContentHeaders = response.Content.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
                    ttfb.Stop();

                    result.StatusCode = (int)response.StatusCode;
                    result.ContentType = response.Content.Headers.ContentType?.MediaType;
                    result.HttpVersion = response.Version.ToString();
                    result.ServerHeader = response.Headers.TryGetValues("server", out var sv) ? sv.FirstOrDefault() : null;
                    result.XPoweredBy = response.Headers.TryGetValues("x-powered-by", out var xp) ? xp.FirstOrDefault() : null;
                    result.TtfbMs = (int)Math.Round(ttfb.Elapsed.TotalMilliseconds);

                    var stopWatch = Stopwatch.StartNew();
                    html = await response.Content.ReadAsStringAsync();
                    stopWatch.Stop();
                    result.LoadTime = stopWatch.Elapsed;
                    result.HtmlBytes = System.Text.Encoding.UTF8.GetByteCount(html);

                    result.Headers = new SecurityHeaders
                    {
                        HasCSP = HasHeader(response, "Content-Security-Policy"),
                        HasHSTS = HasHeader(response, "Strict-Transport-Security"),
                        HasXFO = HasHeader(response, "X-Frame-Options"),
                        HasXCTO = HasHeader(response, "X-Content-Type-Options"),
                        HasReferrerPolicy = HasHeader(response, "Referrer-Policy"),
                        HasPermissionsPolicy = HasHeader(response, "Permissions-Policy")
                    };
                }

                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                string? GetMeta(string name) =>
                    doc.DocumentNode.SelectSingleNode($"//meta[@name='{name}']")?.GetAttributeValue("content", null);
                string? GetProp(string prop) =>
                    doc.DocumentNode.SelectSingleNode($"//meta[@property='{prop}']")?.GetAttributeValue("content", null);

                result.Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim()
                    ?? GetProp("og:title") ?? GetMeta("twitter:title");
                result.Description = GetMeta("description") ?? GetProp("og:description") ?? GetMeta("twitter:description");
                result.CanonicalUrl = doc.DocumentNode.SelectSingleNode("//link[@rel='canonical']")?.GetAttributeValue("href", null);
                result.FaviconUrl = doc.DocumentNode.SelectSingleNode("//link[@rel='icon']")?.GetAttributeValue("href", null)
                             ?? doc.DocumentNode.SelectSingleNode("//link[@rel='shortcut icon']")?.GetAttributeValue("href", null);

                result.LinksCount = doc.DocumentNode.SelectNodes("//a")?.Count ?? 0;
                result.ScriptsCount = doc.DocumentNode.SelectNodes("//script")?.Count ?? 0;
                result.ImageCount = doc.DocumentNode.SelectNodes("//img")?.Count ?? 0;

                var isHttps = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                if (isHttps)
                {
                    result.MixedContent = doc.DocumentNode.SelectNodes("//*[@src or @href]")
                    ?.Any(n =>
                    {
                        var v = n.GetAttributeValue("src", null) ?? n.GetAttributeValue("href", null);
                        return v != null && v.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
                    }) == true;
                }

                var tls = await TryGetTlsInfoAsync(url);

                if (tls is not null)
                {
                    result.TlsProtocol = tls.Value.protocol;
                    result.CertSubject = tls.Value.subject;
                    result.CertIssuer = tls.Value.issuer;
                    result.CertNotAfter = tls.Value.notAfter;
                    result.CertDaysToExpiry = (int)Math.Round((tls.Value.notAfter - DateTimeOffset.UtcNow).TotalDays);
                }

                result.IsSafe = !url.Contains("phishing", StringComparison.OrdinalIgnoreCase)
                    && !url.Contains("malware", StringComparison.OrdinalIgnoreCase);

                result.RiskScore = ComputeRiskScore(url, result);
            }
            catch
            {
                result.IsSafe = false;
                result.RiskScore = 90;
            }

            return result;
        }

        static bool HasHeader(HttpResponseMessage responseMessage, string name) =>
            responseMessage.Headers.Contains(name) || responseMessage.Content.Headers.Contains(name);

        static int ComputeRiskScore(string url, LinkScanResult result)
        {
            int score = 0;

            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) score += 40;
            if ((result.StatusCode ?? 200) >= 400) score += 25;
            if (result.Headers is { HasCSP: false }) score += 5;
            if (result.Headers is { HasHSTS: false } && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) score += 5;
            if (string.IsNullOrWhiteSpace(result.Title)) score += 5;
            if (result.MixedContent) score += 10;
            if ((result.CertDaysToExpiry ?? 365) < 14) score += 10;
            return Math.Min(100, score);
        }

        static async Task<List<string>> GetRedirectsAsync(string url, int maxHops = 5)
        {
            var list = new List<string>();
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

            var current = url;

            for (int i = 0; i < maxHops; i++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, current);
                using var response = await client.SendAsync(request);

                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is Uri location)
                {
                    var next = location.IsAbsoluteUri 
                        ? location.ToString() 
                        : new Uri(new Uri(current), location).ToString();

                    var validation = await UrlSafetyValidator.ValidateAsync(next);

                    if (!validation.IsValid)
                    {
                        list.Add($"{(int)response.StatusCode} -> BLOCKED: {validation.Error}");
                        break;
                    }
                    
                    list.Add($"{(int)response.StatusCode} -> {next}");
                    current = next;
                }
                else break;
            }
            return list;
        }

        static async Task<List<string>> ResolveIpsAsync(string url)
        {
            try
            {
                var host = new Uri(url).Host;
                var ips = await Dns.GetHostAddressesAsync(host);
                return ips.Select(i => i.ToString()).Distinct().ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        static async Task<(string protocol, string subject, string issuer, DateTimeOffset notAfter)?> TryGetTlsInfoAsync(string url)
        {
            try
            {
                var uri = new Uri(url);
                if (uri.Scheme != Uri.UriSchemeHttps) return null;

                using var client = new TcpClient();
                await client.ConnectAsync(uri.Host, 443);
                using var stream = client.GetStream();
                using var ssl = new SslStream(stream, false, new RemoteCertificateValidationCallback((_, cert, _, _) => cert != null));
                await ssl.AuthenticateAsClientAsync(uri.Host);
                var cert2 = new X509Certificate2(ssl.RemoteCertificate!);
                return (ssl.SslProtocol.ToString(), cert2.Subject, cert2.Issuer, cert2.NotAfter);
            }
            catch
            {
                return null;
            }
        }
    }
}