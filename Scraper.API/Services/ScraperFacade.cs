using Scraper.Core.Services;
using Scraper.Core.Helpers;
using Scraper.Bypass.Services;
using Scraper.Bypass.Helpers;

namespace Scraper.API.Services;

public class ScraperFacade
{
    private readonly HttpScraperService _http = new();
    //private readonly PlaywrightService _js = new();

    public async Task<object> ScrapeAsync(string url, string selector, bool useJs)
    {
        string html;

        if (useJs)
        {
            return new
            {
                success = false,
                message = "JavaScript scraping temporarily disabled (Playwright not installed)"
            };
        }
        else
            html = await _http.GetHtmlAsync(url);

        // detect protection
        if (Scraper.Bypass.Helpers.ProtectionDetector.IsBlocked(html))
        {
            return new
            {
                success = false,
                message = "Site is protected (CAPTCHA / Cloudflare detected)"
            };
        }

        var parser = new HtmlParserHelper();
        var data = parser.ExtractByXPath(html, selector);

        return new
        {
            success = true,
            count = data.Count,
            results = data
        };
    }
}