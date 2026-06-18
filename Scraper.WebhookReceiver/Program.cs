using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var dataDirectory = Path.Combine(app.Environment.ContentRootPath, "data");
var eventLogPath = Path.Combine(dataDirectory, "events.ndjson");
Directory.CreateDirectory(dataDirectory);

app.MapGet("/health", () => Results.Ok(new
{
    ok = true,
    service = "Scraper.WebhookReceiver",
    time = DateTime.UtcNow
}));

app.MapPost("/webhook/live-update", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var rawBody = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(rawBody))
    {
        return Results.BadRequest(new { accepted = false, message = "Request body was empty." });
    }

    object? payload;
    try
    {
        payload = JsonSerializer.Deserialize<JsonElement>(rawBody);
    }
    catch (JsonException ex)
    {
        return Results.BadRequest(new { accepted = false, message = $"Invalid JSON payload: {ex.Message}" });
    }

    var record = new
    {
        receivedAtUtc = DateTime.UtcNow,
        payload
    };

    await File.AppendAllTextAsync(eventLogPath, JsonSerializer.Serialize(record) + Environment.NewLine);
    return Results.Ok(new
    {
        accepted = true,
        message = "Live update received.",
        receivedAtUtc = record.receivedAtUtc
    });
});

app.MapGet("/events", async () =>
{
    if (!File.Exists(eventLogPath))
    {
        return Results.Ok(Array.Empty<object>());
    }

    var items = new List<JsonElement>();
    var lines = await File.ReadAllLinesAsync(eventLogPath);
    foreach (var line in lines.Where(line => !string.IsNullOrWhiteSpace(line)))
    {
        try
        {
            items.Add(JsonSerializer.Deserialize<JsonElement>(line));
        }
        catch
        {
            // Skip malformed lines instead of failing the whole response.
        }
    }

    return Results.Ok(items);
});

app.Run("http://localhost:3001");
