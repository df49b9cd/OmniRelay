# REFDISC-008 - HTTP Middleware & Diagnostics Registry

## Goal
Centralize HTTP middleware (auth, logging, tracing, rate limiting) and diagnostics endpoint registration so dispatcher and control-plane services can share the same cross-cutting functionality without duplicating pipeline composition.

## Scope
- Extract reusable middleware registrations from `OmniRelay.Transport.Http.Middleware` and expose them via DI builders similar to gRPC interceptor registries.
- Provide configuration-driven toggles for enabling logging, auth, and tracing middleware per host.
- Offer helper methods to register diagnostics endpoints (`/omnirelay/control/*`) with consistent authorization + serialization.
- Ensure middleware can attach to both dispatcher REST APIs and standalone control-plane hosts built via REFDISC-006.

## Requirements
1. **Composable registry** - Middleware must be registerable by type with ordering guarantees and scope support, avoiding dispatcher-specific assumptions.
2. **Config toggles** - Expose options binding so operators can enable/disable middleware (e.g., auth) without code changes.
3. **Diagnostics parity** - `/omnirelay/control/logging`, `/tracing`, `/lease-health`, `/peers` endpoints need to function identically regardless of host.
4. **Auth hooks** - Support plugging in shared authentication/authorization policies, including mTLS-derived identities or token validators.
5. **Telemetry integration** - Middleware must continue emitting current logs/metrics/traces and respect runtime toggles from the diagnostics runtime.

## Deliverables
- Middleware registry + DI extensions under `OmniRelay.Transport.Http`.
- Refactor dispatcher HTTP pipeline to consume the registry.
- Control-plane hosts updated to pull middleware/diagnostics from the registry instead of inlining copies.
- Documentation covering middleware catalog, configuration switches, and diagnostics endpoint usage.

## Acceptance Criteria
- Dispatcher REST APIs maintain existing middleware behavior after migration.
- Control-plane services can add/remove middleware via configuration and observe the same behavior/logging as dispatcher endpoints.
- Diagnostics endpoints run under the registry in both contexts with consistent authorization + serialization.
- Runtime toggles (logging/tracing enablement) propagate across all hosts using the shared registry.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate registry ordering rules, ensuring middleware runs in declared sequence and detects circular dependencies.
- Test configuration toggles to confirm middleware registers conditionally based on options.
- Ensure diagnostics endpoint handlers return expected payloads and handle null dependencies gracefully.

### Integration tests
- Spin up hosts with different middleware combinations, issue authenticated/unauthenticated requests, and verify behavior matches configuration.
- Toggle runtime logging/tracing via `/omnirelay/control` endpoints and confirm middleware reacts immediately.
- Confirm diagnostics endpoints expose identical data sets across dispatcher and control-plane hosts.

### Feature tests
- In OmniRelay.FeatureTests, configure dispatcher + control-plane hosts with the shared registry, then exercise operator flows (log level changes, tracing toggles, diagnostics fetches) to verify parity.
- Inject auth failures to ensure middleware short-circuits requests consistently and emits expected logs.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, stress hosts with middleware-heavy pipelines to ensure no significant latency regressions and that logging/tracing toggles remain responsive.
- Validate diagnostics endpoints remain responsive even when middleware logs/traces are heavily sampled.

## References
- `src/OmniRelay/Transport/Http/Middleware/` - Existing middleware implementations.
- `docs/architecture/service-discovery.md` - Diagnostics + governance requirements.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
