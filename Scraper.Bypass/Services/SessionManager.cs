using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Bypass.Services
{
    public class SessionManager
    {
        public async Task SaveSessionAsync(IBrowserContext context, string path)
        {
            await context.StorageStateAsync(new() { Path = path });
        }

        public async Task<IBrowserContext> LoadSessionAsync(IPlaywright playwright, string path)
        {
            return await playwright.Chromium.LaunchPersistentContextAsync(path, new()
            {
                Headless = false
            });
        }
    }
}
