# REFDISC-015 - Leadership Coordination Primitives

## Goal
Refactor the leadership coordination subsystem (election scheduler, leases, observers) into reusable primitives so control-plane services can participate in or monitor leadership without embedding dispatcher-specific code.

## Scope
- Extract election algorithms, leader lease tracking, and observer interfaces from the existing `mesh:leadership` implementation.
- Provide DI-friendly services for scheduling leader heartbeats, evaluating quorum, and notifying subscribers of leadership changes.
- Support pluggable persistence (in-memory, external stores) while keeping defaults consistent with current behavior.
- Document how dispatcher and auxiliary services integrate with the primitives.

## Requirements
1. **Deterministic elections** - Maintain current election guarantees (single leader per cluster scope, configurable priority) with the refactored primitives.
2. **Lease tracking** - Leader leases must enforce expiration/renewal semantics and expose diagnostics when leadership changes or lapses.
3. **Observer model** - Components must be able to subscribe to leadership changes with filtering (cluster, role) and reliable delivery.
4. **Telemetry** - Emit metrics/logs for elections, renewals, failures, and conflicts using the shared telemetry module.
5. **Configuration** - Honor existing `mesh:leadership` settings (intervals, priorities) through the shared configuration kit.

## Deliverables
- Leadership primitive library (`OmniRelay.ControlPlane.Leadership`) with election scheduler, lease manager, observer interfaces.
- Refactor existing leadership host to consume the primitives.
- Control-plane services updated to subscribe/publish leadership events via the new library.
- Documentation covering configuration, integration patterns, and operational guidance.

## Acceptance Criteria
- Leadership behavior (election timing, failover) matches pre-refactor results in integration tests.
- Control-plane services can observe leadership events or participate in elections without referencing dispatcher assemblies.
- Telemetry and diagnostics endpoints reflect leadership state and transitions consistently.
- Configuration knobs continue to work identically via the shared configuration kit.
- Library introduces no dispatcher-specific dependencies.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Cover election scoring/prioritization logic, ensuring deterministic leader selection and tie-breaking.
- Test lease renewal/expiration handling under varying clock skews (using TimeProvider abstractions).
- Validate observer notification sequencing and filtering.

### Integration tests
- Spin up multiple nodes using the primitives, run elections/failovers, and assert leadership transitions + diagnostics.
- Simulate network partitions to ensure elections behave according to configured quorum rules.
- Verify telemetry metrics/logs reflect leadership events.

### Feature tests
- In OmniRelay.FeatureTests, integrate the primitives into dispatcher and auxiliary services, then run leader handoff workflows to confirm coordinated behavior.
- Ensure operator diagnostics (CLI, `/control/leadership`) read from the new library and show accurate information.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, run large clusters with the primitives, inject rolling failures, and ensure elections remain stable and timely.
- Measure lease renewal scalability and observer notification latency at scale.

## References
- Existing leadership coordination code (refer to `mesh:leadership` services and docs).
- REFDISC-034..037 - AOT readiness baseline and CI gating.
