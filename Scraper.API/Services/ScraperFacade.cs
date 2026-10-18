using Scraper.Core.Services;
using Scraper.Core.Helpers;
using Scraper.Core.Models;
using Scraper.Bypass.Helpers;

namespace Scraper.API.Services;

public class ScraperFacade
{
    private readonly HttpScraperService _http = new();

    public async Task<object> ScrapeAsync(string url, string selector, bool useJs)
    {
        string html;

        if (useJs)
        {
            try
            {
                await using var playwright = new PlaywrightService();
                html = await playwright.GetHtmlAsync(url);
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = $"JavaScript scraping error: {ex.Message}. Make sure Playwright browsers are installed with 'playwright.ps1 install'."
                };
            }
        }
        else
        {
            try
            {
                html = await _http.GetHtmlAsync(url);
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = $"HTTP request failed: {ex.Message}"
                };
            }
        }

        // detect protection
        if (ProtectionDetector.IsBlocked(html))
        {
            return new
            {
                success = false,
                message = "Site is protected (CAPTCHA / Cloudflare detected)"
            };
        }

        var selectorHelper = new SelectorHelper();
        var kind = (selector.StartsWith('/') || selector.StartsWith("./")) ? SelectorKind.XPath : SelectorKind.Css;
        var data = selectorHelper.ExtractValues(html, selector, kind);

        return new
        {
            success = true,
            count = data.Count,
            results = data
        };
    }
}