using LinkScanner.Application.Abstractions;
using LinkScanner.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace LinkScanner.Infrastructure.Scanning.Analyzers;

public sealed class RedirectAnalyzer
{
    private readonly ILogger<RedirectAnalyzer> _logger;
    private readonly IUrlSafetyValidator _urlSafetyValidator;
    private readonly IRedirectHttpClient _redirectHttpClient;

    public RedirectAnalyzer(ILogger<RedirectAnalyzer> logger, IUrlSafetyValidator urlSafetyValidator, IRedirectHttpClient redirectHttpClient)
    {
        _logger = logger;
        _urlSafetyValidator = urlSafetyValidator;
        _redirectHttpClient = redirectHttpClient;
    }

    public async Task<List<string>> AnalyzeAsync(string url, CancellationToken cancellationToken = default, int maxHops = 5)
    {
        var list = new List<string>();
        var current = url;

        for(var i = 0; i < maxHops; i++)
        {
            var response = await _redirectHttpClient.SendAsync(current, cancellationToken);

            if(response.Location is not { } location)
                break;
            
            var next = location.IsAbsoluteUri
                ? location.ToString()
                : new Uri(new Uri(current), location).ToString();

            var validation = await _urlSafetyValidator.ValidateAsync(next, cancellationToken);

            if(!validation.IsValid)
            {
                list.Add($"{response.StatusCode} -> BLOCKED: {validation.ErrorMessage}");

                _logger.LogWarning("Blocked redirect. CurrentUrl: {CurrentUrl}, NextUrl: {NextUrl}, Reason: {Reason}", 
                    current,
                    next,
                    validation.ErrorMessage);

                break;
            }

            list.Add($"{response.StatusCode} => {next}");
            current = next;
        }

        _logger.LogDebug("Redirect analysis finished for URL: {Url}. Redirect count: {RedirectCount}", url, list.Count);

        return list;
    }
}