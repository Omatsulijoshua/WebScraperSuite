# Node/Express Webhook Receiver

This is a simple receiver for updates sent by the `Automatic Scraper`.

## Endpoints

- `GET /health`
- `POST /webhook/live-update`
- `GET /events`

## Setup

Run these commands inside this folder:

```powershell
npm install
npm start
```

The server will start on:

```text
http://localhost:3001
```

Use this webhook URL in the Automatic Scraper:

```text
http://localhost:3001/webhook/live-update
```

## What gets stored

Incoming payloads are appended to:

```text
data/events.ndjson
```

Each line is one JSON record with:

- `receivedAtUtc`
- `payload`

## Example payload

```json
{
  "sourceUrl": "https://example.com/live",
  "selector": ".score",
  "selectorType": "CSS",
  "fieldName": "Home Score",
  "capturedAtUtc": "2026-05-02T12:34:56Z",
  "values": ["2", "1"],
  "combinedValue": "2 | 1",
  "contentHash": "abc123"
}
```
