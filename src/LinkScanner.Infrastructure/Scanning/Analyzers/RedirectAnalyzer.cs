using LinkScanner.Application.Abstractions;
using LinkScanner.Infrastructure.Scanning.Http;
using LinkScanner.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkScanner.Infrastructure.Scanning.Analyzers;

public sealed class RedirectAnalyzer
{
    private readonly ILogger<RedirectAnalyzer> _logger;
    private readonly IUrlSafetyValidator _urlSafetyValidator;
    private readonly IRedirectHttpClient _redirectHttpClient;
    private readonly LinkScannerOptions _options;

    public RedirectAnalyzer(ILogger<RedirectAnalyzer> logger,
    IUrlSafetyValidator urlSafetyValidator,
    IRedirectHttpClient redirectHttpClient,
    IOptions<LinkScannerOptions> options)
    {
        _logger = logger;
        _urlSafetyValidator = urlSafetyValidator;
        _redirectHttpClient = redirectHttpClient;
        _options = options.Value;
    }

    public async Task<List<string>> AnalyzeAsync(string url, CancellationToken cancellationToken = default, int maxHops = 5)
    {
        var redirects = new List<string>();
        var current = url;

        for (var i = 0; i < _options.MaxRedirects; i++)
        {
            var response = await _redirectHttpClient.SendAsync(current, cancellationToken);

            if (response.Location is not { } location)
                break;

            var next = location.IsAbsoluteUri
                ? location.ToString()
                : new Uri(new Uri(current), location).ToString();

            var validation = await _urlSafetyValidator.ValidateAsync(next, cancellationToken);

            if (!validation.IsValid)
            {
                redirects.Add($"{response.StatusCode} -> BLOCKED: {validation.ErrorMessage}");

                _logger.LogWarning("Blocked redirect. CurrentUrl: {CurrentUrl}, NextUrl: {NextUrl}, Reason: {Reason}",
                    current,
                    next,
                    validation.ErrorMessage);

                break;
            }

            redirects.Add($"{response.StatusCode} -> {next}");
            current = next;
        }

        _logger.LogDebug("Redirect analysis finished for URL: {Url}. Redirect count: {RedirectCount}", url, redirects.Count);

        return redirects;
    }
}