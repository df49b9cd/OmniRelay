# Observability & CLI Playground

Hands-on environment for SREs/operators to exercise OmniRelay diagnostics. The sample hosts HTTP + gRPC inbounds with `/omnirelay/introspect`, `/healthz`, `/readyz`, OpenTelemetry traces/metrics (Prometheus scraping endpoint), and a background service that simulates `omnirelay` CLI scripts hitting the dispatcher at regular intervals.

## Run

```bash
dotnet run --project samples/Observability.CliPlayground --environment Development
```

By default:

- HTTP endpoints: `http://127.0.0.1:7130` (`/omnirelay/introspect`, `/healthz`, `/readyz`, `/metrics`, `/`)
- gRPC inbound: `http://127.0.0.1:7131`
- Prometheus endpoint: `http://127.0.0.1:7130/metrics`

## CLI helpers

Use the bundled scripts or raw commands from `docs/reference/cli-scripts` to validate the dispatcher.

```bash
# Health probe
omnirelay request \
  --transport http \
  --url http://127.0.0.1:7130/healthz \
  --service samples.observability-cli \
  --procedure omnirelay::health \
  --encoding application/json

# Introspection summary
omnirelay introspect --url http://127.0.0.1:7130/omnirelay/introspect --format text

# Replay script
omnirelay script run --file docs/reference/cli-scripts/observability-playground.json
```

## Configuration

- `appsettings*.json` controls hosting URLs, OmniRelay diagnostics, and script cadence (`playground.scriptInterval`).
- Environment overrides via `OBS_CLI__` prefix.
- OpenTelemetry config lives inside `Program.cs` (console exporter for traces + built-in Prometheus exporter).
