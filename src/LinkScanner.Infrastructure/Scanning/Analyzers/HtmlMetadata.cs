namespace LinkScanner.Infrastructure.Scanning.Analyzers;

public sealed class HtmlMetadata
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? FaviconUrl { get; init; }

    public int LinksCount { get; init; }
    public int ScriptsCount { get; init; }
    public int ImageCount { get; init; }

    public bool MixedContent { get; init; }
}