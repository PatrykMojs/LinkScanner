using Microsoft.AspNetCore.Mvc;
using LinkScanner.Domain.Entities;
using LinkScannerApp.Services;

namespace LinkScannerApp.Controllers
{
    public class ScanController : ControllerBase
    {
        private readonly LinkScannerService scanner;

        public ScanController(LinkScannerService service)
        {
            this.scanner = scanner;
        }

        [HttpPost]
        public async Task<ActionResult<LinkScanResult>> Post([FromBody] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("Url is required");

            var result = await scanner.ScanAsync(url);
            return Ok(result);
        }
    }
}