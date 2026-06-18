using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Bypass.Services
{
    public class HumanBehaviorService
    {
        public static async Task SimulateAsync(Microsoft.Playwright.IPage page)
        {
            var random = new Random();

            // random mouse movement
            await page.Mouse.MoveAsync(random.Next(100, 500), random.Next(100, 500));

            // scroll down slowly
            await page.Mouse.WheelAsync(0, random.Next(300, 800));

            // wait like a human
            await page.WaitForTimeoutAsync(random.Next(1000, 3000));
        }
    }
}
