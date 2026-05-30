namespace LinkScanner.Infrastructure.Scanning.Http;

public sealed record RedirectHttpResult(int StatusCode, Uri? Location);