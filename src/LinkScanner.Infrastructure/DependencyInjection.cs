using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using LinkScanner.Infrastructure.Scanning;
using LinkScanner.Infrastructure.Scanning.Analyzers;
using LinkScanner.Infrastructure.Scanning.Http;
using LinkScanner.Infrastructure.Validation;
using LinkScanner.Application.Abstractions;
using LinkScanner.Application.Options;
using LinkScanner.Application.Abstractions.Caching;
using LinkScanner.Infrastructure.Caching;

namespace LinkScanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LinkScannerOptions>(configuration.GetSection(LinkScannerOptions.SectionName));

        services.AddMemoryCache();
        services.AddSingleton<IScanResultCache, InMemoryScanResultCache>();

        services.AddScoped<ILinkScanner, LinkScannerService>();
        services.AddScoped<IUrlSafetyValidator, UrlSafetyValidator>();
        services.AddSingleton<IDnsResolver, SystemDnsResolver>();

        services.AddScoped<SecurityHeadersAnalyzer>();
        services.AddScoped<HostIpResolver>();
        services.AddScoped<RiskScoreCalculator>();
        services.AddScoped<RedirectAnalyzer>();
        services.AddScoped<TlsCertificateAnalyzer>();
        services.AddScoped<HtmlMetadataExtractor>();
        services.AddScoped<SafetyDecisionAnalyzer>();

        services
            .AddHttpClient<HttpPageFetcher>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<LinkScannerOptions>>()
                    .Value;

                client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("LinkScannerApp/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

        services
            .AddHttpClient<IRedirectHttpClient, RedirectHttpClient>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<LinkScannerOptions>>()
                    .Value;

                client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("LinkScannerApp/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });


        return services;
    }
}