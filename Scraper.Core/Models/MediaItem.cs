namespace Scraper.Core.Models;

public class MediaItem
{
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "Ready";
    public string FileName { get; set; } = string.Empty;
}
