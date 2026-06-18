using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace Scraper.UI.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;

        public ApiService()
        {
            _client = new HttpClient();
        }

        public async Task<object?> ScrapeAsync(object request)
        {
            var response = await _client.PostAsJsonAsync(
                "https://localhost:7174/api/scraper/scrape",
                request
            );

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<object>();
        }
    }
}
