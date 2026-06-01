using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using LinkScanner.Application.Abstractions;
using LinkScanner.Infrastructure.Scanning;
using LinkScanner.Infrastructure.Scanning.Analyzers;
using LinkScanner.Infrastructure.Scanning.Http;
using LinkScanner.Infrastructure.Validation;
using LinkScanner.Application.Options;
using Microsoft.Extensions.Options;

namespace LinkScanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LinkScannerOptions>(configuration.GetSection(LinkScannerOptions.SectionName));
       
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

        services.AddSingleton<IDnsResolver, SystemDnsResolver>();

        return services;
    }
}