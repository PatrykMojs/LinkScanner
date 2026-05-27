using LinkScanner.Application.Abstractions;
using LinkScanner.Infrastructure.Scanning;
using LinkScanner.Infrastructure.Scanning.Analyzers;
using Microsoft.Extensions.DependencyInjection;
using LinkScanner.Infrastructure.Scanning.Http;
using LinkScanner.Infrastructure.Validation;

namespace LinkScanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ILinkScanner, LinkScannerService>();
        services.AddHttpClient<HttpPageFetcher>();

        services.AddScoped<IUrlSafetyValidator, UrlSafetyValidator>();

        services.AddScoped<SecurityHeadersAnalyzer>();
        services.AddScoped<HostIpResolver>();
        services.AddScoped<RiskScoreCalculator>();
        services.AddScoped<RedirectAnalyzer>();
        services.AddScoped<TlsCertificateAnalyzer>();
        services.AddScoped<HtmlMetadataExtractor>();
        services.AddScoped<SafetyDecisionAnalyzer>();
        services.AddScoped<HttpPageFetcher>();

        return services;
    }
}