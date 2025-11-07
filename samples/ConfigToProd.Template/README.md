# Config-to-Prod Template Sample

This sample shows how to host OmniRelay via `AddOmniRelayDispatcher` with layered configuration (`appsettings.json`, environment-specific overrides, and environment variables), diagnostics toggles, and HTTP endpoints that plug directly into Docker/Kubernetes liveness and readiness probes. Use it as a starting point for production services that need consistent dispatcher wiring across dev, staging, and prod.

## Layout

| File | Purpose |
| --- | --- |
| `Program.cs` | ASP.NET Core host that loads layered configuration, registers OmniRelay via DI, exposes `/healthz` + `/readyz`, and reacts to diagnostics toggles. |
| `appsettings*.json` | Baseline, Development, and Production defaults for hosting URLs, OmniRelay transport bindings, diagnostics, and probe behavior. |
| `OpsHandlers` | Registers `ops::ping` (unary) and `ops::heartbeat` (oneway) procedures after configuration has materialized the dispatcher. |
| `ProbeState` | Tracks warm-up and diagnostics requirements so `/readyz` only passes after the dispatcher is live and toggles meet policy. |

## Running locally

```bash
dotnet run --project samples/ConfigToProd.Template --environment Development
```

Outputs:

- ASP.NET Core host (REST + probes): `http://127.0.0.1:6050`
- OmniRelay HTTP inbound: `http://127.0.0.1:7082`
- OmniRelay gRPC inbound: `http://127.0.0.1:7092`

Override settings with environment variables:

```bash
export CONFIG2PROD_hosting__urls="http://0.0.0.0:7000"
export CONFIG2PROD_omnirelay__inbounds__http__0__urls__0="http://0.0.0.0:8000"
dotnet run --project samples/ConfigToProd.Template
```

## Probes for containers

- **Liveness:** `GET /healthz` (returns 200 as long as the host and dispatcher process are alive).
- **Readiness:** `GET /readyz` (returns 200 only after the dispatcher warm-up completes and diagnostics toggles satisfy policy). Use this for Kubernetes readiness probes or Docker `HEALTHCHECK`.

Example Kubernetes snippet:

```yaml
livenessProbe:
  httpGet:
    path: /healthz
    port: 6050
  initialDelaySeconds: 10
  periodSeconds: 15
readinessProbe:
  httpGet:
    path: /readyz
    port: 6050
  initialDelaySeconds: 20
  periodSeconds: 10
```

## OmniRelay CLI examples

```bash
# Inspect dispatcher based on configuration
omnirelay introspect --url http://127.0.0.1:7082/omnirelay/introspect --format text

# Unary ping over HTTP
omnirelay request \
  --transport http \
  --url http://127.0.0.1:7082/yarpc/v1 \
  --service samples.config-to-prod \
  --procedure ops::ping \
  --encoding application/json \
  --body '{"message":"config template"}'

# Unary ping over gRPC
omnirelay request \
  --transport grpc \
  --address http://127.0.0.1:7092 \
  --service samples.config-to-prod \
  --procedure ops::ping \
  --encoding application/json \
  --body '{"message":"grpc check"}'

# Oneway heartbeat
omnirelay request \
  --transport http \
  --url http://127.0.0.1:7082/yarpc/v1 \
  --service samples.config-to-prod \
  --procedure ops::heartbeat \
  --encoding application/json \
  --body '{"message":"ready to deploy"}'
```

## Technical notes

- **Layered configuration:** `Program.cs` explicitly loads `appsettings.json`, `appsettings.{Environment}.json`, environment variables, and CLI args with reload-on-change enabled, mirroring how production services consume config packs or ConfigMaps.
- **Diagnostics toggles:** `DiagnosticsToggleWatcher` listens to `IOptionsMonitor<DiagnosticsControlOptions>` so flipping `diagnostics.runtimeMetricsEnabled` at runtime immediately updates logging and readiness requirements.
- **Probes as policy:** `ProbeOptions.readyAfter` gates readiness until OmniRelay is warm (5 seconds by default) and `requireDiagnosticsToggle` ensures `/readyz` fails when diagnostics must be enabled (handy for staging gates).
- **Dispatcher health check:** `DispatcherHealthCheck` plugs into ASP.NET Core Health Checks so default `/healthz` reflects the OmniRelay dispatcher status in addition to the process health.
