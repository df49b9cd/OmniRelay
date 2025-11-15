# OmniRelay Transport Runtime Story

## Goal
- Keep OmniRelay focused on delivering a resilient transport fabric (HTTP/3, HTTP/2, gRPC, middleware, codecs) that hosts embed for RPC workloads while delegating concurrency primitives to Hugo and control-plane duties to MeshKit.

## Scope
- Projects under `src/OmniRelay/*` (Core, Dispatcher, Transport, Middleware, Configuration, CLI).
- Includes codec pipelines, middleware (rate limiting, tracing, deadlines), transport instrumentation, dispatcher extensibility, and CLI/build tooling.
- Excludes low-level concurrency (Hugo) and mesh/leadership/backpressure (MeshKit).

## Requirements
1. Maintain HTTP/gRPC transports with HTTP/3-first support plus HTTP/2 downgrade (`docs/reference/http-transport.md`, `docs/reference/http3-developer-guide.md`).
2. Ensure middleware stack (deadline, retry, rate limiting, tracing, logging) integrates with Hugo’s monitor signals and MeshKit’s control-plane events.
3. Provide dispatcher registration APIs for unary, streaming, resource-lease procedures with deterministic logging/shadowing hooks (`src/OmniRelay/Dispatcher/*.cs`).
4. Keep CLI experience cohesive (profiles, HTTP/3 flags, diagnostics) referencing `docs/reference/cli*.md`.
5. Maintain instrumentation: metrics, tracing, error adapters (e.g., `GrpcTransportMetrics`, `HttpTransportMetrics`, `OmniRelayErrorAdapter`).
6. Sustain configuration pipeline via `OmniRelay.Configuration` with JSON specs, sharding helpers, and host extensions.
7. Expose extensibility points for MeshKit integration (backpressure listeners, replication sinks).

## Deliverables
- Updated OmniRelay packages referencing latest Hugo/MeshKit versions.
- Documentation updates (HTTP, CLI, diagnostics, deterministic guides) reflecting new responsibilities split.
- Reference samples (e.g., `samples/Quickstart.Server`, `samples/ResourceLease.MeshDemo`) showcasing OmniRelay + MeshKit wiring.
- CI validation (dotnet build/test) against .NET SDK pinned in `global.json`.

## Acceptance Criteria
1. OmniRelay builds/tests succeed with updated dependencies.
2. Transport diagnostics (metrics/tracing) remain stable; dashboards ingest new tags without schema churn.
3. Middleware integrates with MeshKit signals (rate-limit selector, diagnostics endpoints) via documented hooks.
4. CLI commands continue to manage transports, show HTTP/3 status, and surface diagnostics.
5. Resource lease dispatcher defers to MeshKit-provided backpressure/replication primitives without breaking API contracts.

## References
- `docs/reference/http-transport.md`, `docs/reference/http3-developer-guide.md`, `docs/reference/http3-faq.md`
- `docs/reference/cli.md`, `docs/reference/cli-tool-readme.md`
- `docs/reference/distributed-task-leasing.md`
- `src/OmniRelay/Dispatcher/*`, `src/OmniRelay/Transport/*`, `src/OmniRelay/Core/Middleware/*`
- `tests/OmniRelay.*`

## Testing Strategy
- Continue running `dotnet build OmniRelay.slnx` and relevant `dotnet test` suites (unit, feature, hyperscale).
- Integration tests covering HTTP/gRPC transports, middleware, deterministic workflows.
- Feature tests via samples, CLI smoke tests, and backpressure control-plane exercises.

### Unit Test Scenarios
- Middleware behaviour (deadline, retry, rate limit) under nominal/error conditions.
- Codec serialization/deserialization (JSON, Protobuf, Raw) with new deterministic contexts.
- Transport error mapping via `OmniRelayErrorAdapter`.

### Integration Test Scenarios
- Dispatcher invoking HTTP + gRPC calls end-to-end (including streaming).
- Resource lease flows using MeshKit backpressure/replication connectors.
- CLI commands exercising diagnostics endpoints and HTTP/3 toggles.

### Feature Test Scenarios
- Samples running in containerized environment with MeshKit + Hugo to validate overall transport fabric.
- Hyperscale feature suites (`tests/OmniRelay.HyperscaleFeatureTests`) ensuring regression-free behaviour.
- Observability drills verifying metrics/traces feed dashboards and alerting.

