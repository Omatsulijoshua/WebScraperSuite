using HtmlAgilityPack;
using Scraper.Core.Helpers;
using Scraper.Core.Models;

namespace Scraper.Core.Services;

public class MediaDiscoveryService
{
    private readonly HtmlParserHelper _parser = new();

    public List<MediaItem> Discover(string html, bool includeImages, bool includeVideos, bool includeAudio)
        => Discover(html, null, includeImages, includeVideos, includeAudio);

    public List<MediaItem> Discover(string html, string? baseUrl, bool includeImages, bool includeVideos, bool includeAudio)
    {
        var document = _parser.Load(html);
        var items = new List<MediaItem>();
        var baseUri = TryCreateBaseUri(baseUrl);

        if (includeImages)
        {
            items.AddRange(ReadImages(document, "//img", baseUri));
        }

        if (includeVideos)
        {
            items.AddRange(ReadMedia(document, "//video[@src] | //source[contains(@type,'video')]", "Video", "src", baseUri));
        }

        if (includeAudio)
        {
            items.AddRange(ReadMedia(document, "//audio[@src] | //source[contains(@type,'audio')]", "Audio", "src", baseUri));
        }

        return items
            .GroupBy(item => item.Url)
            .Select(group => group.First())
            .ToList();
    }

    private static IEnumerable<MediaItem> ReadImages(HtmlDocument document, string xpath, Uri? baseUri)
    {
        var nodes = document.DocumentNode.SelectNodes(xpath)?.Cast<HtmlNode>().ToList() ?? [];
        foreach (var node in nodes)
        {
            var rawUrl = GetImageSrc(node);
            if (string.IsNullOrWhiteSpace(rawUrl) || rawUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var url = ResolveUrl(rawUrl, baseUri);

            yield return new MediaItem
            {
                Url = url,
                Type = "Image",
                FileName = TryGetFileName(url)
            };
        }
    }

    private static string GetImageSrc(HtmlNode node)
    {
        var attributesToTry = new[] { "data-src", "data-lazy-src", "data-original", "data-srcset", "srcset", "src" };
        foreach (var attr in attributesToTry)
        {
            var val = node.GetAttributeValue(attr, string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(val))
            {
                if (val.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (attr.Contains("srcset", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = val.Split(',');
                    if (parts.Length > 0)
                    {
                        var lastPart = parts.Last().Trim();
                        var urlPart = lastPart.Split(' ').FirstOrDefault()?.Trim() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(urlPart))
                        {
                            return urlPart;
                        }
                    }
                }
                else
                {
                    return val;
                }
            }
        }
        return node.GetAttributeValue("src", string.Empty).Trim();
    }

    private static IEnumerable<MediaItem> ReadMedia(HtmlDocument document, string xpath, string type, string attribute, Uri? baseUri)
    {
        var nodes = document.DocumentNode.SelectNodes(xpath)?.Cast<HtmlNode>().ToList() ?? [];
        foreach (var node in nodes)
        {
            var rawUrl = node.GetAttributeValue(attribute, string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                continue;
            }

            var url = ResolveUrl(rawUrl, baseUri);

            yield return new MediaItem
            {
                Url = url,
                Type = type,
                FileName = TryGetFileName(url)
            };
        }
    }

    private static Uri? TryCreateBaseUri(string? baseUrl)
        => Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ? uri : null;

    private static string ResolveUrl(string url, Uri? baseUri)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        if (url.StartsWith("//", StringComparison.Ordinal) && baseUri is not null)
        {
            return $"{baseUri.Scheme}:{url}";
        }

        if (baseUri is not null && Uri.TryCreate(baseUri, url, out var combinedUri))
        {
            return combinedUri.ToString();
        }

        return url;
    }

    private static string TryGetFileName(string url)
    {
        try
        {
            return Path.GetFileName(new Uri(url, UriKind.RelativeOrAbsolute).LocalPath);
        }
        catch
        {
            return Path.GetFileName(url);
        }
    }
}
