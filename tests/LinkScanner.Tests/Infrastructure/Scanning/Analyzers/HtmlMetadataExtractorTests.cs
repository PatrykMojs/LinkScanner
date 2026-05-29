using FluentAssertions;
using LinkScanner.Infrastructure.Scanning.Analyzers;

namespace LinkScanner.Tests.Infrastructure.Scanning.Analyzers;

public sealed class HtmlMetadataExtractorTests
{
    private readonly HtmlMetadataExtractor _extractor = new();

    [Fact]
    public void Extract_ShouldReturnTitle_FromTitleTag()
    {
        var html = """
            <html>
                <head>
                    <title>Example page</title>
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.Title.Should().Be("Example page");
    }

    [Fact]
    public void Extract_ShouldTrimTitle_WhenTitleContainsWhitespace()
    {
        var html = """
            <html>
                <head>
                    <title>   Example page   </title>
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.Title.Should().Be("Example page");
    }

    [Fact]
    public void Extract_ShouldUseOgTitle_WhenTitleTagIsMissing()
    {
        var html = """
            <html>
                <head>
                    <meta property="og:title" content="Open Graph title">
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.Title.Should().Be("Open Graph title");
    }

    [Fact]
    public void Extract_ShouldUseTwitterTitle_WhenTitleAndOgTitleAreMissing()
    {
        var html = """
            <html>
                <head>
                    <meta name="twitter:title" content="Twitter title">
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.Title.Should().Be("Twitter title");
    }

    [Fact]
    public void Extract_ShouldReturnDescription_FromMetaDescription()
    {
        var html = """
            <html>
                <head>
                    <meta name="description" content="Example description">
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.Description.Should().Be("Example description");
    }

    [Fact]
    public void Extract_ShouldUseOgDescription_WhenMetaDescriptionIsMissing()
    {
        var html = """
            <html>
                <head>
                    <meta property="og:description" content="Open Graph description">
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.Description.Should().Be("Open Graph description");
    }

    [Fact]
    public void Extract_ShouldUseTwitterDescription_WhenMetaAndOgDescriptionAreMissing()
    {
        var html = """
            <html>
                <head>
                    <meta name="twitter:description" content="Twitter description">
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.Description.Should().Be("Twitter description");
    }

    [Fact]
    public void Extract_ShouldReturnCanonicalUrl_WhenCanonicalLinkExists()
    {
        var html = """
            <html>
                <head>
                    <link rel="canonical" href="https://example.com/page">
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.CanonicalUrl.Should().Be("https://example.com/page");
    }

    [Fact]
    public void Extract_ShouldReturnFaviconUrl_WhenIconLinkExists()
    {
        var html = """
            <html>
                <head>
                    <link rel="icon" href="/favicon.ico">
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.FaviconUrl.Should().Be("/favicon.ico");
    }

    [Fact]
    public void Extract_ShouldReturnFaviconUrl_WhenShortcutIconLinkExists()
    {
        var html = """
            <html>
                <head>
                    <link rel="shortcut icon" href="/shortcut.ico">
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.FaviconUrl.Should().Be("/shortcut.ico");
    }

    [Fact]
    public void Extract_ShouldCountLinksScriptsAndImages()
    {
        var html = """
            <html>
                <head>
                    <script src="/app.js"></script>
                    <script src="/analytics.js"></script>
                </head>
                <body>
                    <a href="/one">One</a>
                    <a href="/two">Two</a>
                    <a href="/three">Three</a>

                    <img src="/one.png">
                    <img src="/two.png">
                </body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.LinksCount.Should().Be(3);
        result.ScriptsCount.Should().Be(2);
        result.ImageCount.Should().Be(2);
    }

    [Fact]
    public void Extract_ShouldDetectMixedContent_WhenHttpsPageContainsHttpImage()
    {
        var html = """
            <html>
                <body>
                    <img src="http://example.com/image.png">
                </body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.MixedContent.Should().BeTrue();
    }

     [Fact]
    public void Extract_ShouldDetectMixedContent_WhenHttpsPageContainsHttpLink()
    {
        var html = """
            <html>
                <body>
                    <a href="http://example.com/login">Login</a>
                </body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.MixedContent.Should().BeTrue();
    }

    [Fact]
    public void Extract_ShouldNotDetectMixedContent_WhenPageUrlUsesHttp()
    {
        var html = """
            <html>
                <body>
                    <img src="http://example.com/image.png">
                </body>
            </html>
            """;

        var result = _extractor.Extract(html, "http://example.com");

        result.MixedContent.Should().BeFalse();
    }

    [Fact]
    public void Extract_ShouldReturnNullMetadata_WhenHtmlDoesNotContainMetadata()
    {
        var html = """
            <html>
                <head></head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html, "https://example.com");

        result.Title.Should().BeNull();
        result.Description.Should().BeNull();
        result.CanonicalUrl.Should().BeNull();
        result.FaviconUrl.Should().BeNull();
        result.LinksCount.Should().Be(0);
        result.ScriptsCount.Should().Be(0);
        result.ImageCount.Should().Be(0);
        result.MixedContent.Should().BeFalse();
    }
}