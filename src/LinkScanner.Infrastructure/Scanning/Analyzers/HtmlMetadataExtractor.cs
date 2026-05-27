using HtmlAgilityPack;

namespace LinkScanner.Infrastructure.Scanning.Analyzers;

public sealed class HtmlMetadataExtractor
{
    public HtmlMetadata Extract(string html, string url)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        string? GetMeta(string name)
        {
            return document.DocumentNode
                .SelectSingleNode($"//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{name.ToLowerInvariant()}']")
                ?.GetAttributeValue("content", null)
                ?.Trim();
        }

        string? GetProperty(string property)
        {
            return document.DocumentNode
                .SelectSingleNode($"//meta[translate(@property,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{property.ToLowerInvariant()}']")
                ?.GetAttributeValue("content", null)
                ?.Trim();
        }

        var title = document.DocumentNode
            .SelectSingleNode("//title")
            ?.InnerText
            ?.Trim()
            ?? GetProperty("og:title")
            ?? GetMeta("twitter:title");

        var description =
            GetMeta("description")
            ?? GetProperty("og:description")
            ?? GetMeta("twitter:description");

        var canonical = document.DocumentNode
            .SelectSingleNode("//link[translate(@rel,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='canonical']")
            ?.GetAttributeValue("href", null);

        var favicon = document.DocumentNode
            .SelectSingleNode("//link[translate(@rel,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='icon']")
            ?.GetAttributeValue("href", null)
            ?? document.DocumentNode
                .SelectSingleNode("//link[translate(@rel,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='shortcut icon']")
                ?.GetAttributeValue("href", null);

        var links = document.DocumentNode
            .SelectNodes("//a")
            ?.Count ?? 0;

        var scripts = document.DocumentNode
            .SelectNodes("//script")
            ?.Count ?? 0;

        var images = document.DocumentNode
            .SelectNodes("//img")
            ?.Count ?? 0;

        var mixedContent = IsHttps(url) && HasMixedContent(document);

        return new HtmlMetadata
        {
            Title = title,
            Description = description,
            CanonicalUrl = canonical,
            FaviconUrl = favicon,
            LinksCount = links,
            ScriptsCount = scripts,
            ImageCount = images,
            MixedContent = mixedContent
        };
    }

    private static bool IsHttps(string url)
    {
        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasMixedContent(HtmlDocument document)
    {
        return document.DocumentNode
            .SelectNodes("//*[@src or @href]")
            ?.Any(node =>
            {
                var value = node.GetAttributeValue("src", null)
                            ?? node.GetAttributeValue("href", null);

                return value is not null &&
                       value.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            }) == true;
    }
}