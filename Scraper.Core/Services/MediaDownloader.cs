namespace Scraper.Core.Services;

public class MediaDownloader
{
    private readonly HttpClient _client = new();

    public async Task DownloadAsync(string url, string path, CancellationToken cancellationToken = default)
    {
        var bytes = await _client.GetByteArrayAsync(url, cancellationToken);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
    }
}
