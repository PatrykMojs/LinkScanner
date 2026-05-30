
namespace LinkScanner.Infrastructure.Scanning.Http;

public sealed class RedirectHttpClient : IRedirectHttpClient
{
    private readonly HttpClient _httpClient;

    public RedirectHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<RedirectHttpResult> SendAsync(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        return new RedirectHttpResult((int)response.StatusCode, response.Headers.Location);
    }
}