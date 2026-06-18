using System.Net.Http;

namespace Scraper.Core.Services;

public class HttpScraperService
{
    private readonly HttpClient _client;

    public HttpScraperService(HttpClient? client = null)
    {
        _client = client ?? new HttpClient();

        if (!_client.DefaultRequestHeaders.UserAgent.Any())
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "WebScraperPro/1.0 (+desktop)");
        }
    }

    public Task<string> GetHtmlAsync(string url, CancellationToken cancellationToken = default) =>
        _client.GetStringAsync(url, cancellationToken);

    public Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default) =>
        _client.GetByteArrayAsync(url, cancellationToken);
}
