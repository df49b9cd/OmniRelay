# REFDISC-018 - Chaos & Health Probe Infrastructure

## Goal
Refactor the dispatcher-centric chaos injection and synthetic probe infrastructure into reusable services so control-plane components can register health probes, inject faults, and observe results uniformly.

## Scope
- Extract probe registration, scheduling, and result aggregation logic from existing dispatcher test harnesses.
- Provide APIs to define health probes (latency checks, dependency calls) and chaos experiments (latency injection, packet loss).
- Expose telemetry/diagnostics endpoints for probe results and chaos status.
- Document how operators/services configure probes and chaos scenarios.

## Requirements
1. **Probe abstraction** - Support synchronous/asynchronous probes with configurable intervals, timeouts, and thresholds.
2. **Chaos controls** - Allow enabling/disabling chaos experiments per host with safety checks and observability.
3. **Telemetry integration** - Emit metrics/logs for probe successes/failures, chaos activations, and recovery.
4. **Security** - Restrict chaos controls to authorized operators, leveraging diagnostics runtime auth hooks.
5. **Extensibility** - Allow new probe types/chaos actions to be registered via DI without modifying core code.

## Deliverables
- Probe/chaos infrastructure library (`OmniRelay.Diagnostics.Probes`).
- Dispatcher refactor to use the shared infrastructure.
- Control-plane services wired to register probes (e.g., gossip health) and optional chaos hooks.
- Documentation outlining probe configuration, chaos usage, and safety considerations.

## Acceptance Criteria
- Existing synthetic probe dashboards and chaos tooling continue functioning post-migration.
- Control-plane services can register probes and expose results via shared diagnostics endpoints.
- Chaos experiments can be toggled per host with proper authorization and logging.
- Telemetry from probes/chaos flows through the unified telemetry module.
- Infrastructure has no dispatcher-specific dependencies, enabling reuse in tests and production hosts.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate probe scheduling, timeout handling, and failure threshold logic.
- Test chaos configuration parsing and safety checks (e.g., require confirmation tokens).
- Ensure telemetry counters increment appropriately for probe/chaos events.

### Integration tests
- Configure probes against test endpoints, verify results surface via diagnostics and metrics.
- Run chaos experiments (e.g., inject latency) and ensure toggles/logs behave as expected.
- Confirm authorization prevents unauthorized chaos activation.

### Feature tests
- In OmniRelay.FeatureTests, register probes for dispatcher and control-plane services, run chaos workflows, and verify operator tooling observes expected behavior.
- Validate failure scenarios (probe timeouts) trigger alerts/metrics consistently.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, manage hundreds of probes across nodes, ensuring scheduling scales and chaos controls remain responsive.
- Stress chaos toggles to ensure they don’t destabilize hosts beyond configured blast radius.

## References
- Existing synthetic probe/chaos tooling in tests (`tests/OmniRelay.HyperscaleFeatureTests`, diagnostics docs).
- REFDISC-034..037 - AOT readiness baseline and CI gating.
