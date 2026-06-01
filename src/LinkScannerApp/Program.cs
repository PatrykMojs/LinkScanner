using System.Threading.RateLimiting;
using LinkScanner.Application;
using LinkScanner.Infrastructure;
using LinkScannerApp.Extensions;
using LinkScannerApp.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting LinkScanner application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    });

    builder.Services.AddRazorPages();
    builder.Services.AddControllers();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.Configure<AppRateLimitOptions>(
    builder.Configuration.GetSection(AppRateLimitOptions.SectionName));

    builder.Services.Configure<RequestLimitsOptions>(builder.Configuration.GetSection(RequestLimitsOptions.SectionName));

    var rateLimitOptions = builder.Configuration
        .GetSection(AppRateLimitOptions.SectionName)
        .Get<AppRateLimitOptions>() ?? new AppRateLimitOptions();

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("ScanPolicy", httpContext =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ip,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.ScanPermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = rateLimitOptions.QueueLimit
                });
        });

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, token) =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("RateLimiter");

            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.HttpContext.Request.Path.ToString();
            var method = context.HttpContext.Request.Method;
            var userAgent = context.HttpContext.Request.Headers.UserAgent.ToString();

            context.HttpContext.Response.Headers.RetryAfter = rateLimitOptions.WindowSeconds.ToString();

            logger.LogWarning("Rate limit exceeded. IP: {IpAddress}, Method: {Method}, Path: {Path}, UserAgent: {UserAgent}, RetryAfterSeconds: {RetryAfterSeconds}",
                ip,
                method,
                path,
                userAgent,
                rateLimitOptions.WindowSeconds);

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/problem+json";

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                title = "Too many requests.",
                status = StatusCodes.Status429TooManyRequests,
                detail = "Wykonano zbyt wiele skanów. Spróbuj ponownie za chwilę.",
                retryAfterSeconds = rateLimitOptions.WindowSeconds,
                traceId = context.HttpContext.TraceIdentifier
            }, token);
        };
    });

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseGlobalExceptionHandling();

    app.UseHttpsRedirection();
    app.UseSecurityHeaders();

    app.UseRequestBodySizeLimit();

    app.UseStaticFiles();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseRouting();

    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapControllers();
    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "LinkScanner application terminated unexpectedly");
}
finally
{
    Log.Information("Stopping LinkScanner application");
    Log.CloseAndFlush();
}