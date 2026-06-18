using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Scraper.UI.Models;


namespace Scraper.UI.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http = new();

        public async Task<ScrapeResponse?> ScrapeAsync(object request)
        {
            var response = await _http.PostAsJsonAsync(
                "https://localhost:7174/api/scraper/scrape",
                request
            );

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ScrapeResponse>();
        }
    }
}
