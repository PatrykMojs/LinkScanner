namespace LinkScanner.Infrastructure.Scanning.Http;

public interface IRedirectHttpClient
{
    Task<RedirectHttpResult> SendAsync(string url, CancellationToken cancellationToken = default);
}