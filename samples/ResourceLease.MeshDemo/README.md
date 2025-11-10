# ResourceLease Mesh Demo

An end-to-end sample that highlights the OmniRelay ResourceLease RPC mesh feature set: durable replication, deterministic capture, peer health tracking, backpressure hooks, diagnostics control plane, and background workers that lease work through the canonical `resourcelease::*` procedures.

## What it hosts

| Component | Description |
| --- | --- |
| ResourceLease dispatcher | Runs on `http://127.0.0.1:7420/yarpc/v1` (service `resourcelease-mesh-demo`, namespace `resourcelease.mesh`). |
| Durable replication | `SqliteResourceLeaseReplicator` persists events to `mesh-data/replication.db` and mirrors them to an in-memory log served at `/demo/replication`. |
| Deterministic store | `SqliteDeterministicStateStore` captures effect ids in `mesh-data/deterministic.db`. |
| Backpressure hooks | `BackpressureAwareRateLimiter` + diagnostics listener keep the HTTP worker pool in sync with SafeTaskQueue backpressure. |
| Diagnostics endpoints | `/demo/lease-health`, `/demo/backpressure`, `/demo/replication`, `/demo/enqueue` (human-friendly helpers layered on top of `/omnirelay/control/*`). |
| Background services | `LeaseSeederHostedService` enqueues synthetic work; `LeaseWorkerHostedService` leases/heartbeats/complete/fail items to showcase replication + peer metrics. |

## Run it

```bash
dotnet run --project samples/ResourceLease.MeshDemo
```

Outputs:

- ResourceLease RPC endpoint: `http://127.0.0.1:7420/yarpc/v1`
- Diagnostics UI: `http://localhost:5158/` (default ASP.NET port; see console)

## Interact with the dispatcher

### Enqueue work via HTTP helper

```bash
curl -X POST http://localhost:5158/demo/enqueue \
  -H "Content-Type: application/json" \
  -d '{ "resourceType":"demo.order","resourceId":"external-cli","partitionKey":"tenant-42" }'
```

### Enqueue via OmniRelay CLI

```bash
omnirelay request \
  --transport http \
  --url http://127.0.0.1:7420/yarpc/v1 \
  --service resourcelease-mesh-demo \
  --procedure resourcelease.mesh::enqueue \
  --encoding application/json \
  --body '{"payload":{"resourceType":"demo.order","resourceId":"cli","partitionKey":"tenant-cli","payloadEncoding":"application/json","body":"eyJtZXNzYWdlIjoiY2xpIn0="}}'
```

### Inspect health + replication

```bash
curl http://localhost:5158/demo/lease-health      # PeerLeaseHealthTracker snapshot
curl http://localhost:5158/demo/backpressure      # Latest SafeTaskQueue backpressure signal
curl http://localhost:5158/demo/replication       # Recent replication events from SQLite
```

## Configuration

- `appsettings.json` (`meshDemo` section) controls:
  - `rpcUrl`: HTTP inbound for ResourceLease RPCs.
  - `dataDirectory`: where SQLite replication/deterministic files are stored.
  - `workerPeerId`: peer identifier used by the background worker.
  - `seederIntervalSeconds`: cadence for seeding demo work.
- Override any value via environment variables prefixed with `MESHDEMO_` (e.g., `MESHDEMO_meshDemo__rpcUrl`).

## Concepts showcased

- Resource-neutral `ResourceLease*` contracts.
- Durable replication + deterministic stores without external dependencies (SQLite).
- `PeerLeaseHealthTracker` diagnostics exposed over HTTP.
- Backpressure-aware rate limiting via `BackpressureAwareRateLimiter` + `RateLimitingBackpressureListener`.
- CLI-friendly helper endpoints for drain/restore/introspection workflows.
- Background worker that exercises `resourcelease.mesh::{lease,heartbeat,complete,fail}` to demonstrate requeue + replication lag metrics.
