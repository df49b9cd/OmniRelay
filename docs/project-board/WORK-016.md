# WORK-016 – MeshKit.Cross-Cluster Replication & Failover

## Goal
Use MeshKit.ClusterDescriptors plus replication streams to orchestrate planned and emergency failovers across clusters, emitting telemetry and CLI workflows while OmniRelay remains a stateless transport fabric.

## Scope
- Enhance replication services to stream logs between clusters with `ClusterVersionVector` metadata, lag metrics, and idempotent replay.
- Build planned failover workflow (drain primary, promote passive, update routing metadata atomically) and emergency workflow (force promote with fencing tokens + warnings).
- Provide CLI commands (`mesh clusters promote`, `mesh clusters failback`, `mesh replication status`) hitting MeshKit automation endpoints.
- Document runbooks, diagrams, and rollback steps.

## Requirements
1. **Ordering & dedupe** – Maintain per-cluster monotonic sequencing, support replays, and guard against double-applying updates.
2. **Security** – Replication channels use HTTP/3/gRPC with mTLS + optional attestation.
3. **Automation** – Integrate with change management approvals, pre-flight checks (lag, health), and audit logging.
4. **Observability** – Metrics for lag, active/passive state, workflow progress; alerts for lag thresholds and failover failures.
5. **Testing** – Chaos scenarios simulating regional loss/failover must meet documented SLO (<30s for core control-plane APIs).

## Deliverables
- Replication enhancements, failover controllers, CLI workflows, documentation/runbooks.

## Acceptance Criteria
- Replication lag metrics accurate ±5%; dashboards/alerts wired.
- Planned failover completes within SLO, updates routing metadata, and logs audit trail.
- Emergency failover forcibly promotes passive cluster with fencing tokens; clients resume operations quickly.
- Native AOT builds/tests per WORK-002..WORK-005.

## Testing Strategy
- Unit tests for version-vector math, workflow state machines, serialization compatibility.
- Integration tests with multi-cluster setups measuring lag, executing planned/emergency failovers, verifying telemetry + audit.
- Feature tests running runbook drills for planned failover and failback.
- Hyperscale tests performing concurrent failovers across regions to validate automation + alerting scale.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`