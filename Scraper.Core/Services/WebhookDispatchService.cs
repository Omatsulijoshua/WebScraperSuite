using System.Net.Http.Json;

namespace Scraper.Core.Services;

public class WebhookDispatchService
{
    private readonly HttpClient _client = new();

    public async Task DispatchJsonAsync<TPayload>(string webhookUrl, TPayload payload, CancellationToken cancellationToken = default)
    {
        using var response = await _client.PostAsJsonAsync(webhookUrl, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
