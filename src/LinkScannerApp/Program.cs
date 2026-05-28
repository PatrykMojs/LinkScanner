using System.Threading.RateLimiting;
using LinkScanner.Application;
using LinkScanner.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
   configuration.ReadFrom.Configuration(context.Configuration); 
});

builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

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
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Wykonano zbyt wiele skanów. Spróbuj ponownie za chwilę.",
            token);
    };
});

var app = builder.Build();

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
