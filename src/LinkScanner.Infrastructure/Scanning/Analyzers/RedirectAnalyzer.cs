using LinkScanner.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LinkScanner.Infrastructure.Scanning.Analyzers;

public sealed class RedirectAnalyzer
{
    private readonly ILogger<RedirectAnalyzer> _logger;
    private readonly IUrlSafetyValidator _urlSafetyValidator;

    public RedirectAnalyzer(ILogger<RedirectAnalyzer> logger, IUrlSafetyValidator urlSafetyValidator)
    {
        _logger = logger;
        _urlSafetyValidator = urlSafetyValidator;
    }

    public async Task<List<string>> AnalyzeAsync(string url, CancellationToken cancellationToken = default, int maxHops = 5)
    {
        var list = new List<string>();
        var current = url;

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };

        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("LinkScannerApp/1.0");

        for(var i = 0; i < maxHops; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            using var response = await client.SendAsync(request, cancellationToken);

            if(response.Headers.Location is not { } location)
                break;

            var next = location.IsAbsoluteUri
                ? location.ToString()
                : new Uri(new Uri(current), location).ToString();

            var validation = await _urlSafetyValidator.ValidateAsync(next, cancellationToken);

            if(!validation.IsValid)
            {
                list.Add($"{(int)response.StatusCode} -> BLOCKED: {validation.ErrorMessage}");
                
                _logger.LogWarning("Blocked redirect. CurrentUrl: {CurrentUrl}, NextUrl: {NextUrl}, Reason: {Reason}",
                    current,
                    next,
                    validation.ErrorMessage);

                break;
            }

            list.Add($"{(int)response.StatusCode} -> {next}");
            current = next;
        }

        _logger.LogDebug("Redirect analysis finished for URL: {Url}. Redirect count: {RedirectCount}", url, list.Count);

        return list;
    }
}