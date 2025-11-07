# Samples Backlog

Curated backlog of sample projects that demonstrate OmniRelay end-to-end patterns. Each entry highlights the target audience, the runtime slices it exercises, and any critical teaching goals.

## Minimal API Bridge (shipped)

- Implemented at `samples/MinimalApiBridge` with docs in `docs/reference/samples.md`.
- Demonstrates ASP.NET Core Minimal APIs and OmniRelay sharing the same Generic Host, DI registrations, and handler classes for both REST and RPC traffic.

## Config-to-Prod Template (shipped)

- Implemented at `samples/ConfigToProd.Template`, covering layered configuration, diagnostics toggles, `/healthz` + `/readyz`, and OmniRelay hosting via `AddOmniRelayDispatcher`.

## Streaming Analytics Lab

- **Status:** Shipped at `samples/StreamingAnalytics.Lab`.
- **Highlights:** JSON ticker streams, Protobuf client/duplex handlers, and loopback streaming clients that exercise OmniRelay codecs and backpressure end-to-end.

## Observability & CLI Playground (shipped)

- Implemented at `samples/Observability.CliPlayground` with HTTP/gRPC endpoints, Prometheus/OpenTelemetry wiring, and CLI scripts under `docs/reference/cli-scripts/observability-playground.json`.

## Codegen + Tee Rollout Harness

- **Status:** Shipped at `samples/CodegenTee.Rollout`.
- **Highlights:** Builds `risk.proto` via the OmniRelay generator, registers the generated service, and tees typed client calls to primary + shadow deployments for safe rollout rehearsals.

## Multi-Tenant Gateway Sample

- **Status:** Shipped at `samples/MultiTenant.Gateway`.
- **Highlights:** Demonstrates tenant routing via headers, per-tenant quota/logging middleware, and tenant-specific HTTP outbounds.

## Hybrid Batch + Realtime Runner

- **Status:** Shipped at `samples/HybridRunner`.
- **Highlights:** Oneway batch ingestion, background worker progress publishing, and server-stream dashboards sharing the same middleware stack.

## Chaos & Failover Lab

- **Status:** Shipped at `samples/ChaosFailover.Lab`.
- **Highlights:** Flaky backends, retry/deadline middleware, and a traffic generator demonstrate failover behavior while engineers inspect diagnostics.
