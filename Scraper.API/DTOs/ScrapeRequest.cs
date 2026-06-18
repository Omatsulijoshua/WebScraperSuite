namespace Scraper.API.DTOs
{
    public class ScrapeRequest
    {
        public string Url { get; set; }
        public string Selector { get; set; }
        public bool UseJavaScript { get; set; }
    }
}
