using LinkScanner.Application.UseCases.ScanUrl;
using Microsoft.Extensions.DependencyInjection;

namespace LinkScanner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ScanUrlHandler>();

        return services;
    }
}