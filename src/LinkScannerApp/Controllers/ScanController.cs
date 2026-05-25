using LinkScanner.Application.UseCases.ScanUrl;
using LinkScanner.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LinkScannerApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ScanPolicy")]
public class ScanController : ControllerBase
{
    private readonly ScanUrlHandler scanUrlHandler;

    public ScanController(ScanUrlHandler scanUrlHandler)
    {
        this.scanUrlHandler = scanUrlHandler;
    }

    [HttpPost]
    public async Task<ActionResult<LinkScanResult>> Post([FromBody] string url, CancellationToken cancellationToken)
    {
        var response = await scanUrlHandler.HandleAsync(new ScanUrlCommand(url), cancellationToken);

        if (!response.IsSuccess)
        {
            return BadRequest(new
            {
                error = response.ErrorMessage
            });
        }

        return Ok(response.Result);
    }
}