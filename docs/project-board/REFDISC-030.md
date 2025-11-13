# REFDISC-030 - Runtime Bootstrap Harness

## Goal
Encapsulate process bootstrap responsibilities (feature flag evaluation, environment detection, telemetry/logging initialization) into a reusable harness so dispatcher and control-plane services start consistently across environments.

## Scope
- Extract bootstrap logic from dispatcher Program.cs/startup scripts, including feature flag evaluation, host configuration, and global.json enforcement checks.
- Provide a harness that sets up logging, telemetry, diagnostic services, and feature flags before host creation.
- Support pluggable environment detectors (container, cloud) and hooks for custom startup tasks.
- Document usage patterns for services adopting the harness.

## Requirements
1. **Consistent startup** - Ensure all hosts initialize telemetry, logging, diagnostics, and configuration in the same order with consistent error handling.
2. **Feature flags** - Integrate with feature flag providers to gate experimental functionality at startup.
3. **Environment awareness** - Detect environment (dev/test/prod, container) and apply defaults (e.g., URL bindings) accordingly.
4. **Bootstrap diagnostics** - Emit logs/metrics for startup steps and expose failure reasons.
5. **Extensibility** - Allow additional bootstrap hooks without modifying core harness code.

## Deliverables
- Bootstrap harness library (`OmniRelay.Hosting.Bootstrap`).
- Dispatcher refactor to use the harness in its entry point.
- Control-plane services updated to adopt the harness.
- Documentation detailing configuration, feature flag usage, and troubleshooting.

## Acceptance Criteria
- Dispatcher startup sequence and logging remain unchanged after adopting the harness.
- Control-plane services boot with the same telemetry/logging/feature flag setup without custom code.
- Feature flags can be evaluated consistently across hosts before major services initialize.
- Bootstrap failures emit actionable diagnostics shared across services.
- Harness is dispatcher-agnostic.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate feature flag evaluation order and defaulting logic.
- Test environment detection (env vars, container indicators) mapping to expected profiles.
- Ensure bootstrap hooks run in defined order and handle exceptions.

### Integration tests
- Run sample hosts using the harness under different environments (dev/prod), verifying configuration and binding behavior.
- Toggle feature flags and confirm gated components are enabled/disabled at startup.
- Simulate bootstrap failures to verify logging/exit behavior.

### Feature tests
- In OmniRelay.FeatureTests, start dispatcher/control-plane hosts via the harness and ensure telemetry/logging/diagnostics surfaces match baselines.
- Validate operator workflows that rely on feature flags (enable experimental transport) behave identically.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, perform mass restarts to ensure the harness scales and doesn’t introduce startup contention.
- Measure startup time to confirm harness overhead is minimal even at large scale.

## References
- Existing Program.cs/bootstrap logic in dispatcher and service samples.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
