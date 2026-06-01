using System.Threading.RateLimiting;
using LinkScanner.Application;
using LinkScanner.Infrastructure;
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

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("ScanPolicy", httpContext =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ip,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
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

            logger.LogWarning("Rate limit exceeded. IP: {IpAddress}, Method: {Method}, Path: {Path}, UserAgent: {UserAgent}", ip, method, path, userAgent);

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            await context.HttpContext.Response.WriteAsync("Wykonano zbyt wiele skanów. Spróbuj ponownie za chwilę.", token);
        };
    });

    var app = builder.Build();


    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
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
catch(Exception ex)
{
    Log.Fatal(ex, "LinkScanner application terminated unexpectedly");
}
finally
{
    Log.Information("Stopping LinkScanner application");
    Log.CloseAndFlush();
}