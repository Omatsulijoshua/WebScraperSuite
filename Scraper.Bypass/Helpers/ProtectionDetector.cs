using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Bypass.Helpers
{
    public static class ProtectionDetector
    {
        public static bool IsBlocked(string html)
        {
            var text = html.ToLower();

            return text.Contains("captcha")
                || text.Contains("cloudflare")
                || text.Contains("verify you are human")
                || text.Contains("checking your browser")
                || text.Contains("access denied")
                || text.Contains("bot detection");
        }
    }
}
