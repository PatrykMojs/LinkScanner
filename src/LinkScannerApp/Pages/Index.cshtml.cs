using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LinkScanner.Domain.Entities;
using LinkScannerApp.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace LinkScannerApp.Pages;

[EnableRateLimiting("ScanPolicy")]
public class IndexModel : PageModel
{
    private readonly LinkScannerService scanner;

    public IndexModel(LinkScannerService scanner)
    {
        this.scanner = scanner;
    }

    [BindProperty]
    public string Url { get; set; } = "";
    public LinkScanResult? Result { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!string.IsNullOrWhiteSpace(Url))
        {
            Result = await scanner.ScanAsync(Url);
        }

        return Page();
    }
}
