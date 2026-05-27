using System.Diagnostics;
using System.Text;

namespace LinkScanner.Infrastructure.Scanning.Http;

public sealed class HttpPageFetcher
{
    private readonly HttpClient httpClient;

    public HttpPageFetcher(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        this.httpClient.Timeout = TimeSpan.FromSeconds(8);
        this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LinkScannerApp/1.0");
    }

    public async Task<HttpPageFetchResult> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        var ttfb = Stopwatch.StartNew();

        var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        ttfb.Stop();

        var rawHeaders = response.Headers
            .ToDictionary(header => header.Key, header => string.Join(", ", header.Value));

        var rawContentHeaders = response.Content.Headers
            .ToDictionary(header => header.Key, header => string.Join(", ", header.Value));

        var loadTimer = Stopwatch.StartNew();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        loadTimer.Stop();

        return new HttpPageFetchResult
        {
            Response = response,
            StatusCode = (int)response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.MediaType,
            HttpVersion = response.Version.ToString(),
            ServerHeader = response.Headers.TryGetValues("server", out var serverValues)
                ? serverValues.FirstOrDefault()
                : null,
            XPoweredBy = response.Headers.TryGetValues("x-powered-by", out var poweredByValues)
                ? poweredByValues.FirstOrDefault()
                : null,
            TtfbMs = (int)Math.Round(ttfb.Elapsed.TotalMilliseconds),
            LoadTime = loadTimer.Elapsed,
            HtmlBytes = Encoding.UTF8.GetByteCount(html),
            Html = html,
            RawHeaders = rawHeaders,
            RawContentHeaders = rawContentHeaders
        };
    }
}