using LinkScannerApp.Middleware;

namespace LinkScannerApp.Extensions;

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UsesecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}