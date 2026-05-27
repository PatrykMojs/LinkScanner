using LinkScanner.Application.Abstractions;
using LinkScanner.Infrastructure.Scanning;
using LinkScanner.Infrastructure.Validation;
using LinkScanner.Infrastructure.Scanning.Analyzers;
using Microsoft.Extensions.DependencyInjection;
using LinkScanner.Infrastructure.Scanning.Http;

namespace LinkScanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient<ILinkScanner, LinkScannerService>();
        services.AddHttpClient<HttpPageFetcher>();

        services.AddScoped<SecurityHeadersAnalyzer>();
        services.AddScoped<HostIpResolver>();
        services.AddScoped<RiskScoreCalculator>();
        services.AddScoped<RedirectAnalyzer>();
        services.AddScoped<TlsCertificateAnalyzer>();
        services.AddScoped<HtmlMetadataExtractor>();

        return services;
    }
}