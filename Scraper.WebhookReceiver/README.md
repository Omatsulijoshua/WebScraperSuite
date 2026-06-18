# ASP.NET Webhook Receiver

This is a small local receiver for `Automatic Scraper` updates.

## Run

```powershell
cd C:\Users\USER\source\repos\WebScraperSuite\Scraper.WebhookReceiver
dotnet run
```

The app listens on:

```text
http://localhost:3001
```

## Webhook URL

Use this in the Automatic Scraper:

```text
http://localhost:3001/webhook/live-update
```

## Endpoints

- `GET /health`
- `POST /webhook/live-update`
- `GET /events`

## Stored Events

Incoming events are appended to:

```text
data/events.ndjson
```
