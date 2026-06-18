namespace Scraper.Core.Models;

public class ClonePageResult
{
    public string Url { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public int AssetCount { get; set; }
    public int LinkCount { get; set; }
    public string Status { get; set; } = string.Empty;
}
