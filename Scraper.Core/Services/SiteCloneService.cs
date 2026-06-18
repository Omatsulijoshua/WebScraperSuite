using System.Security.Cryptography;
using System.Text;
using HtmlAgilityPack;
using Scraper.Core.Models;

namespace Scraper.Core.Services;

public class SiteCloneService
{
    private static readonly string[] AssetAttributes = ["src", "href", "poster"];

    public List<string> DiscoverPageLinks(string html, string currentUrl, bool sameOriginOnly = true)
    {
        var links = new List<string>();
        if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var currentUri))
        {
            return links;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        var nodes = document.DocumentNode.SelectNodes("//a[@href]");
        if (nodes is null)
        {
            return links;
        }

        foreach (var node in nodes)
        {
            var href = node.GetAttributeValue("href", string.Empty).Trim();
            var absoluteUrl = ResolveUrl(currentUri, href);
            if (absoluteUrl is null)
            {
                continue;
            }

            if (sameOriginOnly && !IsSameOrigin(currentUri, absoluteUrl))
            {
                continue;
            }

            if (!IsHtmlLike(absoluteUrl))
            {
                continue;
            }

            links.Add(absoluteUrl.ToString());
        }

        return links
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<string> DiscoverAssetLinks(string html, string currentUrl)
    {
        var assets = new List<string>();
        if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var currentUri))
        {
            return assets;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        foreach (var attribute in AssetAttributes)
        {
            var nodes = document.DocumentNode.SelectNodes($"//*[@{attribute}]");
            if (nodes is null)
            {
                continue;
            }

            foreach (var node in nodes)
            {
                var rawValue = node.GetAttributeValue(attribute, string.Empty).Trim();
                var absoluteUrl = ResolveUrl(currentUri, rawValue);
                if (absoluteUrl is null)
                {
                    continue;
                }

                assets.Add(absoluteUrl.ToString());
            }
        }

        return assets
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string RewriteDocumentLinks(
        string html,
        string currentUrl,
        IReadOnlyDictionary<string, string> pageMap,
        IReadOnlyDictionary<string, string> assetMap,
        string outputRoot)
    {
        if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var currentUri))
        {
            return html;
        }

        var currentPageLocalPath = pageMap.TryGetValue(currentUrl, out var localPath)
            ? localPath
            : BuildLocalPagePath(currentUrl, outputRoot);

        var currentPageDirectory = Path.GetDirectoryName(currentPageLocalPath) ?? outputRoot;
        var document = new HtmlDocument();
        document.LoadHtml(html);

        foreach (var attribute in AssetAttributes)
        {
            var nodes = document.DocumentNode.SelectNodes($"//*[@{attribute}]");
            if (nodes is null)
            {
                continue;
            }

            foreach (var node in nodes)
            {
                var rawValue = node.GetAttributeValue(attribute, string.Empty).Trim();
                var absoluteUrl = ResolveUrl(currentUri, rawValue);
                if (absoluteUrl is null)
                {
                    continue;
                }

                if (assetMap.TryGetValue(absoluteUrl.ToString(), out var assetLocalPath))
                {
                    node.SetAttributeValue(attribute, BuildRelativePath(currentPageDirectory, assetLocalPath));
                    continue;
                }

                if (pageMap.TryGetValue(absoluteUrl.ToString(), out var pageLocalPath))
                {
                    node.SetAttributeValue(attribute, BuildRelativePath(currentPageDirectory, pageLocalPath));
                }
            }
        }

        return document.DocumentNode.OuterHtml;
    }

    public string BuildLocalPagePath(string url, string outputRoot)
    {
        var uri = new Uri(url);
        var path = uri.AbsolutePath.Trim('/');
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizeSegment)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToList();

        string fileName;
        string directoryPath;
        if (segments.Count == 0)
        {
            directoryPath = outputRoot;
            fileName = "index.html";
        }
        else
        {
            directoryPath = Path.Combine(outputRoot, Path.Combine(segments.ToArray()));
            fileName = "index.html";
        }

        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            var querySuffix = SanitizeSegment(uri.Query.TrimStart('?').Replace('&', '_').Replace('=', '-'));
            directoryPath = Path.Combine(directoryPath, querySuffix);
        }

        return Path.Combine(directoryPath, fileName);
    }

    public string BuildLocalAssetPath(string assetUrl, string outputRoot)
    {
        var uri = new Uri(assetUrl);
        var extension = Path.GetExtension(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 10)
        {
            extension = ".bin";
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(assetUrl))).ToLowerInvariant();
        return Path.Combine(outputRoot, "assets", $"{hash}{extension}");
    }

    private static Uri? ResolveUrl(Uri currentUri, string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.StartsWith('#') ||
            value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            return Uri.TryCreate($"{currentUri.Scheme}:{value}", UriKind.Absolute, out var protocolRelativeUri)
                ? protocolRelativeUri
                : null;
        }

        return Uri.TryCreate(currentUri, value, out var combinedUri) ? combinedUri : null;
    }

    private static bool IsSameOrigin(Uri left, Uri right)
        => string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
           && left.Port == right.Port;

    private static bool IsHtmlLike(Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        return string.IsNullOrWhiteSpace(extension)
            || extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".htm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".php", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".aspx", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(invalid.Contains(character) ? '_' : character);
        }

        return builder.ToString().Trim('.', ' ');
    }

    private static string BuildRelativePath(string fromDirectory, string toPath)
    {
        var relative = Path.GetRelativePath(fromDirectory, toPath);
        return relative.Replace('\\', '/');
    }
}
