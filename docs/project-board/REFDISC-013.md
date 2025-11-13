# REFDISC-013 - Peer Health & Lease Diagnostics Kit

## Goal
Decouple `PeerLeaseHealthTracker`, snapshot providers, and diagnostics endpoints from dispatcher hosting so any control-plane service can record and expose peer health/lease information uniformly.

## Scope
- Move peer health tracking, metadata enrichment, and snapshot APIs into a shared kit.
- Provide DI registrations for trackers, snapshot providers, and diagnostics transformers (e.g., `PeerLeaseHealthDiagnostics`).
- Ensure gossip/leadership components can record lease/gossip events without referencing dispatcher assemblies.
- Document how to wire the kit into diagnostics endpoints and telemetry.

## Requirements
1. **Shared data model** - Use a common representation for peer health entries, labels, and diagnostics to avoid divergent schemas.
2. **Thread-safe tracking** - Trackers must handle concurrent updates from multiple sources (gossip sweeps, lease events) without locks becoming bottlenecks.
3. **Diagnostics integration** - `/omnirelay/control/lease-health` endpoint must read from the kit regardless of host.
4. **Telemetry** - Emit metrics for peer health state transitions and anomalies using the shared telemetry module.
5. **Extensibility** - Allow additional health providers to plug in (e.g., transport-level health) via interfaces.

## Deliverables
- Peer health kit (tracker, snapshot provider interfaces, diagnostics helper).
- Dispatcher refactor to consume the kit rather than private implementations.
- Control-plane services updated to record/report peer health via the kit.
- Documentation explaining usage, metadata conventions, and extension points.

## Acceptance Criteria
- Existing peer health diagnostics output (JSON format, fields) remains unchanged post-migration.
- Gossip and leadership services can record peer metadata without referencing dispatcher.
- Diagnostics endpoint `/omnirelay/control/lease-health` functions even when dispatcher is offline, as long as the kit is registered.
- Metrics emitted by the tracker align with previous counters (counts per status, event rates).
- Kit introduces no dispatcher-only dependencies.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate tracker behavior under concurrent updates, ensuring snapshots reflect the latest metadata and statuses.
- Test metadata enrichment (labels, mesh-role info) for correctness and case insensitivity.
- Ensure diagnostics transformation handles empty snapshots, large datasets, and null providers gracefully.

### Integration tests
- Wire the kit into a sample host, record gossip + lease events, and verify `/omnirelay/control/lease-health` returns expected data.
- Combine multiple providers to ensure snapshots merge correctly.
- Confirm telemetry counters increment as events are recorded.

### Feature tests
- In OmniRelay.FeatureTests, use the kit across dispatcher and control-plane services, then verify operator workflows (lease health inspection, peer debugging) behave identically.
- Simulate node failures and ensure health diagnostics update promptly regardless of host.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, feed large volumes of health events into the kit to ensure it scales without performance degradation.
- Stress diagnostics queries (frequent `/lease-health` fetches) to ensure snapshot generation remains efficient.

## References
- `src/OmniRelay/Core/Peers/PeerLeaseHealthTracker.cs` - Current implementation to extract.
- `docs/architecture/service-discovery.md` - Peer health observability requirements.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
