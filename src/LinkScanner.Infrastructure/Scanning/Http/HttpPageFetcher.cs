using System.Diagnostics;
using System.Text;
using LinkScanner.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkScanner.Infrastructure.Scanning.Http;

public sealed class HttpPageFetcher
{
    private readonly ILogger<HttpPageFetcher> _logger;
    private readonly HttpClient _httpClient;
    private readonly LinkScannerOptions _options;

    public HttpPageFetcher(ILogger<HttpPageFetcher> logger, 
        HttpClient httpClient, 
        IOptions<LinkScannerOptions> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<HttpPageFetchResult> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        var ttfb = Stopwatch.StartNew();

        var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        ttfb.Stop();

        var rawHeaders = response.Headers
            .ToDictionary(header => header.Key, header => string.Join(", ", header.Value));

        var rawContentHeaders = response.Content.Headers
            .ToDictionary(header => header.Key, header => string.Join(", ", header.Value));

        var loadTimer = Stopwatch.StartNew();

        var htmlBytes = await ReadLimitedContentAsync(
            response.Content,
            _options.MaxHtmlBytes,
            cancellationToken);

        loadTimer.Stop();

        var html = Encoding.UTF8.GetString(htmlBytes);

        _logger.LogDebug("Fetched page. Url: {Url}, StatusCode: {StatusCode}, TTFB: {TtfbMs}ms, LoadTime: {LoadTimeMs}ms, HtmlBytes: {HtmlBytes}",
            url,
            (int)response.StatusCode,
            (int)Math.Round(ttfb.Elapsed.TotalMilliseconds),
            (int)Math.Round(loadTimer.Elapsed.TotalMilliseconds),
            htmlBytes.Length);

        return new HttpPageFetchResult
        {
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
            HtmlBytes = htmlBytes.Length,
            Html = html,
            RawHeaders = rawHeaders,
            RawContentHeaders = rawContentHeaders,
            Response = response
        };
    }

    private static async Task<byte[]> ReadLimitedContentAsync(
        HttpContent content,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var memoryStream = new MemoryStream();

        var buffer = new byte[8192];
        var totalBytes = 0;

        while(true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);

            if(read == 0)
                break;

            totalBytes += read;

            if(totalBytes > maxBytes)
                throw new InvalidOperationException($"HTTP response body exceeded the configured limit of {maxBytes} bytes.");

            memoryStream.Write(buffer, 0, read);
        }

        return memoryStream.ToArray(); 
    }
}