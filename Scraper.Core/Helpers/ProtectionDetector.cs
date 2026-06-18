namespace Scraper.Core.Helpers;

public static class ProtectionDetector
{
    private static readonly string[] Signals =
    [
        "captcha",
        "cloudflare",
        "verify you are human",
        "checking your browser",
        "/cdn-cgi/",
        "challenge-platform"
    ];

    public static bool IsProtected(string html) => GetSignals(html).Count > 0;

    public static IReadOnlyList<string> GetSignals(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return Array.Empty<string>();
        }

        return Signals
            .Where(signal => html.Contains(signal, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
