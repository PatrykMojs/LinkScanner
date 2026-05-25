using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using LinkScanner.Domain.Entities;
using LinkScanner.Application.UseCases.ScanUrl;

namespace LinkScannerApp.Pages;

[EnableRateLimiting("ScanPolicy")]
public class IndexModel : PageModel
{
    private readonly ScanUrlHandler scanUrlHandler;

    public IndexModel(ScanUrlHandler scanUrlHandler)
    {
        this.scanUrlHandler = scanUrlHandler;
    }

    [BindProperty]
    public string InputUrl { get; set; } = "";
    public LinkScanResult? Result { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var response = await scanUrlHandler.HandleAsync(new ScanUrlCommand(InputUrl), HttpContext.RequestAborted);

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Nie udało się przeskanować adresu URL.");
            return Page();
        }

        Result = response.Result;

        return Page();
    }
}
