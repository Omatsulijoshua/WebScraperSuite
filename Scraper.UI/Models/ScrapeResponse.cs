using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.UI.Models
{
    public class ScrapeResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public List<object> Results { get; set; } = new();
    }
}
