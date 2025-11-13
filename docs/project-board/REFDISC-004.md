# REFDISC-004 - Shared Interceptor & Telemetry Registries

## Goal
Decouple the gRPC interceptor registries and transport telemetry plumbing from `Dispatcher` so service-discovery control-plane services can opt into logging, tracing, auth, and metrics without instantiating the dispatcher runtime.

## Scope
- Move `GrpcClientInterceptorRegistry`, `GrpcServerInterceptorRegistry`, and `Composite*Interceptor` types into a standalone package with DI registration helpers.
- Expose builder APIs that let hosts register interceptors with attributes/ordering identical to dispatcher behavior.
- Centralize transport telemetry (meters, activity sources, logging toggles) so non-dispatcher services emit the same signals.
- Update dispatcher initialization to consume the shared registries, proving backwards compatibility.

## Requirements
1. **Parity with dispatcher** - Shared registries must preserve ordering, filtering, and dependency injection semantics currently used inside `Dispatcher`.
2. **Declarative registration** - Allow interceptors to be registered by type with optional annotations (e.g., `StreamingOnly`) so control-plane services can reuse them.
3. **Telemetry hooks** - Provide default meter/activity source registrations and ensure interceptors can emit spans/logs consistently.
4. **Minimal dependencies** - Refactored registries cannot depend on dispatcher-specific concepts (procedures, middleware), keeping them lightweight.
5. **Configuration wiring** - Allow registries to be configured via appsettings (enable logging interceptor, sampling toggles) in both dispatcher and control-plane contexts.

## Deliverables
- Shared interceptor/telemetry package with DI extensions.
- Dispatcher refactor to remove private registry implementations.
- Control-plane hosts (gossip/leadership) updated to opt into interceptors & telemetry via the shared package.
- Documentation describing how to register interceptors and interpret emitted telemetry outside dispatcher.

## Acceptance Criteria
- Dispatcher retains its current interceptor behavior (ordering, enable/disable switches) after migration.
- Control-plane services can add logging/tracing interceptors without referencing `Dispatcher`.
- Telemetry meters/spans appear under the same names (`OmniRelay.Transport.Grpc`, etc.) regardless of host.
- Configuration toggles (enable server logging, adjust sampling) work for both dispatcher and control-plane services via the same keys.
- No additional dependencies are introduced into `OmniRelay.Dispatcher` other than the shared package.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate registry ordering and deduplication logic with combinations of interceptor descriptors.
- Confirm DI resolution works for scoped/singleton interceptors when used outside dispatcher.
- Ensure telemetry helpers register meters/activity sources exactly once even when multiple hosts share the same process.

### Integration tests
- Register logging and tracing interceptors in a control-plane host, execute RPCs, and assert logs/spans materialize with expected metadata.
- Toggle configuration flags via options binding and verify interceptors enable/disable without restarts.
- Confirm dispatcher + control-plane services can coexist in the same process without conflicting interceptor registrations.

### Feature tests
- Within OmniRelay.FeatureTests, enable logging interceptors for both dispatcher data-plane calls and gossip control-plane calls, verifying output parity.
- Ensure tracing spans emitted by interceptors propagate through the diagnostics runtime toggles `/omnirelay/control/tracing`.

### Hyperscale Feature Tests
- In OmniRelay.HyperscaleFeatureTests, deploy many hosts with interceptors enabled and assert telemetry volume remains within acceptable bounds while maintaining ordering/behavior.
- Stress-test dynamic toggles (turning logging on/off) to ensure registries reconfigure cleanly under load.

## References
- `src/OmniRelay/Transport/Grpc/Interceptors/` - Existing interceptor infrastructure.
- `src/OmniRelay/Dispatcher/Dispatcher.cs` - Current registry instantiation call sites.
- `docs/architecture/service-discovery.md` - Observability + governance expectations.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
