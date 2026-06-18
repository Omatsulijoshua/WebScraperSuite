namespace Scraper.Core.Models;

public class ScrapeRequest
{
    public string UrlOrHtml { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public SelectorKind SelectorKind { get; set; } = SelectorKind.Css;
    public ScrapeSiteType SiteType { get; set; } = ScrapeSiteType.Static;
    public bool TreatInputAsRawHtml { get; set; }
}
