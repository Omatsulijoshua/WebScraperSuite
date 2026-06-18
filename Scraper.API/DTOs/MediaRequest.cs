namespace Scraper.API.DTOs
{
    public class MediaRequest
    {
        public string Url { get; set; }
        public bool Images { get; set; }
        public bool Videos { get; set; }
        public bool Audio { get; set; }
    }
}
