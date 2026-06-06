using LinkScannerApp.Options;
using Microsoft.Extensions.Options;

namespace LinkScannerApp.Middleware;

public sealed class RequestBodySizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestBodySizeLimitMiddleware> _logger;
    private readonly RequestLimitsOptions _options;

    public RequestBodySizeLimitMiddleware(RequestDelegate next,
        ILogger<RequestBodySizeLimitMiddleware> logger,
        IOptions<RequestLimitsOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsScanEndpoint(context))
        {
            await _next(context);
            return;
        }

        var contentLength = context.Request.ContentLength;

        if (contentLength.HasValue && contentLength.Value > _options.MaxScanRequestBodyBytes)
        {
            _logger.LogWarning("Request body too large. Path: {Path}, ContentLength: {ContentLength}, MaxAllowedBytes: {MaxAllowedBytes}, TraceId: {TraceId}",
                context.Request.Path,
                contentLength.Value,
                _options.MaxScanRequestBodyBytes,
                context.TraceIdentifier);

            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new
            {
                title = "Request body too large.",
                status = StatusCodes.Status413PayloadTooLarge,
                detail = $"Request body cannot exceed {_options.MaxScanRequestBodyBytes} bytes.",
                traceId = context.TraceIdentifier
            });

            return;
        }

        await _next(context);
    }

    private static bool IsScanEndpoint(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api/scan");
    }
}