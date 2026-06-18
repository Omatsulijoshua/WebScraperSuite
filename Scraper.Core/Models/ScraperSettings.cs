namespace Scraper.Core.Models;

public class ScraperSettings
{
    public string ApiProvider { get; set; } = "Gemini"; // Gemini, OpenAI
    public string ApiModel { get; set; } = "gemini-1.5-flash"; // gemini-1.5-flash, gemini-2.5-flash, gpt-4o-mini, gpt-4o, etc.
    public string ApiKey { get; set; } = string.Empty;
    public string CustomEndpoint { get; set; } = string.Empty;
    public string OutputFolder { get; set; } = string.Empty;
    public bool AutoSaveCsv { get; set; } = true;
    public bool AutoSaveJson { get; set; } = true;
    public bool AutoDownloadImages { get; set; } = true;
    public bool AutoDownloadAudio { get; set; } = true;
    public bool AutoDownloadVideos { get; set; } = true;
    public bool EnableAutopilot { get; set; } = false;
    public int MaxScrollLoops { get; set; } = 5;
    public bool EnableAutoScroll { get; set; } = true;
    public string PreferredBrowser { get; set; } = "Chrome";
    public int ApiTimeoutSeconds { get; set; } = 100;
}
