using LinkScanner.Application.Abstractions;
using LinkScanner.Infrastructure.Scanning;
using LinkScanner.Infrastructure.Validation;
using LinkScanner.Infrastructure.Scanning.Analyzer;
using Microsoft.Extensions.DependencyInjection;

namespace LinkScanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient<ILinkScanner, LinkScannerService>();
        services.AddScoped<IUrlSafetyValidator, UrlSafetyValidator>();

        services.AddScoped<SecurityHeadersAnalyzer>();
        services.AddScoped<HostIpResolver>();
        services.AddScoped<RiskScoreCalculator>();

        return services;
    }
}