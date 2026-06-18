using HtmlAgilityPack;

namespace Scraper.Core.Helpers;

public class HtmlParserHelper
{
    public HtmlDocument Load(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html ?? string.Empty);
        return document;
    }
}
