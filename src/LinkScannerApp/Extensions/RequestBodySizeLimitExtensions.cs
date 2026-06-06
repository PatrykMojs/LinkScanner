using LinkScannerApp.Middleware;

namespace LinkScannerApp.Extensions;

public static class RequestBodySizeLimitExtensions
{
    public static IApplicationBuilder UseRequestBodySizeLimit(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestBodySizeLimitMiddleware>();
    }
}