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
            var node = document.DocumentNode.SelectSingleNode(
                $"//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{name.ToLowerInvariant()}']");

            return GetAttribute(node, "content");
        }

        string? GetProperty(string property)
        {
            var node = document.DocumentNode.SelectSingleNode(
                $"//meta[translate(@property,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{property.ToLowerInvariant()}']");

            return GetAttribute(node, "content");
        }

        var title = document.DocumentNode
            .SelectSingleNode("//title")
            ?.InnerText
            ?.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            title = GetProperty("og:title") ?? GetMeta("twitter:title");
        }

        var description =
            GetMeta("description")
            ?? GetProperty("og:description")
            ?? GetMeta("twitter:description");

        var canonicalNode = document.DocumentNode.SelectSingleNode(
            "//link[translate(@rel,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='canonical']");

        var canonical = GetAttribute(canonicalNode, "href");

        var faviconNode = document.DocumentNode.SelectSingleNode(
            "//link[translate(@rel,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='icon']");

        var shortcutFaviconNode = document.DocumentNode.SelectSingleNode(
            "//link[translate(@rel,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='shortcut icon']");

        var favicon =
            GetAttribute(faviconNode, "href")
            ?? GetAttribute(shortcutFaviconNode, "href");

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
                var src = GetAttribute(node, "src");
                var href = GetAttribute(node, "href");

                var value = src ?? href;

                return value is not null &&
                       value.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            }) == true;
    }

    private static string? GetAttribute(HtmlNode? node, string attributeName)
    {
        if (node is null)
        {
            return null;
        }

        var value = node.GetAttributeValue(attributeName, string.Empty).Trim();

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }
}