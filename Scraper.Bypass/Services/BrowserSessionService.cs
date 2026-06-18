using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Scraper.Bypass.Services
{
    public class BrowserSessionService
    {
        public async Task<IPage> LaunchBrowserAsync(string? storagePath = null)
        {
            var playwright = await Playwright.CreateAsync();

            IBrowserContext context;

            if (!string.IsNullOrEmpty(storagePath) && File.Exists(storagePath))
            {
                context = await playwright.Chromium.LaunchPersistentContextAsync(storagePath, new()
                {
                    Headless = false,
                    SlowMo = 50
                });
            }
            else
            {
                var browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = false,
                    SlowMo = 50
                });

                context = await browser.NewContextAsync();
            }

            var page = await context.NewPageAsync();
            return page;
        }
    }
}
