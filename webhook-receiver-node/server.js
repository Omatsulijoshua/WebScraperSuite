const express = require("express");
const fs = require("fs");
const path = require("path");

const app = express();
const port = process.env.PORT || 3001;
const dataDir = path.join(__dirname, "data");
const logFile = path.join(dataDir, "events.ndjson");

fs.mkdirSync(dataDir, { recursive: true });

app.use(express.json({ limit: "1mb" }));

app.get("/health", (_req, res) => {
  res.json({
    ok: true,
    service: "webscraperpro-webhook-receiver",
    time: new Date().toISOString()
  });
});

app.post("/webhook/live-update", (req, res) => {
  const payload = req.body || {};
  const record = {
    receivedAtUtc: new Date().toISOString(),
    payload
  };

  fs.appendFileSync(logFile, JSON.stringify(record) + "\n", "utf8");

  console.log("Live update received:");
  console.log(JSON.stringify(record, null, 2));

  res.status(200).json({
    accepted: true,
    message: "Live update received.",
    receivedAtUtc: record.receivedAtUtc
  });
});

app.get("/events", (_req, res) => {
  if (!fs.existsSync(logFile)) {
    return res.json([]);
  }

  const lines = fs
    .readFileSync(logFile, "utf8")
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => {
      try {
        return JSON.parse(line);
      } catch {
        return null;
      }
    })
    .filter(Boolean);

  res.json(lines);
});

app.listen(port, () => {
  console.log(`Webhook receiver listening on http://localhost:${port}`);
  console.log(`POST updates to http://localhost:${port}/webhook/live-update`);
  console.log(`Health check: http://localhost:${port}/health`);
});
