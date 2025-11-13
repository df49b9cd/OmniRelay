# REFDISC-009 - Diagnostics Runtime Control Kit

## Goal
Extract the dispatcher’s diagnostics runtime (logging/tracing toggles, runtime state) and `/omnirelay/control/*` endpoint handlers into a reusable kit so any host can expose control knobs without referencing dispatcher internals.

## Scope
- Move `DiagnosticsRuntimeState`, `IDiagnosticsRuntime`, and related options binding into a neutral package.
- Provide helper extensions to register control endpoints (logging level, tracing probability, diagnostics info) on any ASP.NET Core app.
- Ensure runtime state can aggregate inputs from multiple hosts in the same process without conflicts.
- Document how dispatcher, gossip, and leadership services consume the kit.

## Requirements
1. **State management** - Runtime state must remain thread-safe, support dynamic updates, and broadcast changes to subscribers.
2. **Endpoint consistency** - `/omnirelay/control/logging`, `/omnirelay/control/tracing`, `/omnirelay/control/lease-health`, and `/omnirelay/control/peers` must return the same payloads regardless of host.
3. **Authorization hooks** - Allow custom auth policies (mTLS identity, bearer tokens) to protect control endpoints.
4. **Configuration binding** - Respect existing options (`diagnostics.runtime.*`) and allow enabling/disabling the control plane per host.
5. **Extensibility** - Provide a way for new control modules to register endpoints using the same serialization + error-handling patterns.

## Deliverables
- Diagnostics runtime kit (interfaces, state implementation, endpoint registration helpers).
- Dispatcher refactor to consume the kit rather than private implementations.
- Updates to control-plane hosts to register the diagnostics kit endpoints.
- Documentation describing configuration, security, and usage.

## Acceptance Criteria
- Control endpoints behave identically before and after dispatcher refactor (verified via diff of JSON payloads).
- Control-plane hosts can enable logging/tracing toggles independently by referencing the kit.
- Authorization policies can be attached via configuration and enforced consistently.
- Runtime state survives multiple registrations (dispatcher + gossip) inside one host without duplicate endpoints.
- Configuration toggles (`EnableControlPlane`, per-feature flags) work across all consumers.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Verify runtime state updates (set log level, set sampling probability) propagate to subscribers and validate input ranges.
- Test endpoint handlers for null/invalid requests to ensure consistent 400/204 responses.
- Ensure authorization hooks are invoked and failures return 401/403 as configured.

### Integration tests
- Host a test app using the kit, issue control requests (GET/POST) and confirm state changes and JSON payloads.
- Toggle configuration to disable specific endpoints and verify they return 404 or are not mapped.
- Run multiple hosts in one process to confirm endpoints register once and share state.

### Feature tests
- Within OmniRelay.FeatureTests, enable diagnostics endpoints on dispatcher and gossip hosts via the kit, then drive logging/tracing toggles to ensure both react.
- Validate `/omnirelay/control/peers` + `/lease-health` remain accessible and accurate even when the dispatcher is offline.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, stress control endpoints with concurrent toggles (log level thrash) to ensure state remains consistent and latency acceptable.
- Simulate RBAC/mTLS auth requirements to ensure authorization hooks scale with many operators.

## References
- `src/OmniRelay.Configuration/ServiceCollectionExtensions.cs` - Current diagnostics runtime wiring.
- `docs/architecture/service-discovery.md` - Control-plane diagnostics requirements.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
