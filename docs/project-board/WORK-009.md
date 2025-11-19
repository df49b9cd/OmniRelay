# WORK-009 – Runtime Bootstrap Harness

## Goal
Encapsulate startup responsibilities (feature flag evaluation, environment detection, telemetry/logging initialization, diagnostics runtime wiring) into a reusable harness so OmniRelay and MeshKit hosts boot consistently across environments.

## Scope
- Extract bootstrap logic from dispatcher `Program.cs` and MeshKit prototypes into `OmniRelay.Hosting.Bootstrap`.
- Initialize logging (shared enrichers), telemetry modules, diagnostics runtime, feature flags, and configuration binding before host creation.
- Provide pluggable environment detectors (dev/test/prod, container, cloud) and hooks for custom startup tasks.
- Document usage patterns and troubleshooting steps.

## Requirements
1. **Consistent startup sequence** – Telemetry/logging/diagnostics/config load in the same order with consistent error handling across services.
2. **Feature flags** – Integrate with feature-flag providers to gate experimental functionality prior to host building.
3. **Environment awareness** – Detect environment and apply sane defaults (port bindings, TLS paths, logging verbosity).
4. **Bootstrap diagnostics** – Emit logs/metrics for each step, expose failure reasons via diagnostics endpoints, and integrate with CLI.
5. **Extensibility** – Allow additional bootstrap hooks without editing core harness code; maintain trimming/AOT safety.

## Deliverables
- Harness library, tests, sample usage for OmniRelay + MeshKit hosts.
- Documentation describing configuration, feature flags, troubleshooting, and integration with `transport-layer-vision`.

## Acceptance Criteria
- Dispatcher/MeshKit startup remains functionally identical but now flows through the harness.
- Feature flags/environment detection operate consistently across hosts.
- Bootstrap failures emit actionable diagnostics/logs.
- Native AOT builds/tests succeed (WORK-002..WORK-005).

## Testing Strategy
- Unit: feature flag evaluation, environment detection, hook ordering, exception handling.
- Integration: run sample hosts in dev/prod/container contexts verifying configuration + binding behavior.
- Feature: start hosts via harness inside feature tests ensuring telemetry/logging/diagnostics surfaces match expectations.
- Hyperscale: mass restarts to ensure harness scales and doesn’t introduce startup contention.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`
