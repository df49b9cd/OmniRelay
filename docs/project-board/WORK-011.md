# WORK-011 – MeshKit.Rebalancer

## Goal
Introduce MeshKit.Rebalancer as an independent control-plane module that ingests MeshKit telemetry (shards, peer health, SafeTaskQueue backpressure) and produces safe shard movement plans with dry-run/approval workflows. OmniRelay continues to provide only transports and telemetry plumbing.

## Scope
- Build a scoring engine that consumes MeshKit.Shards snapshots, MeshKit peer health, and Hugo backpressure feeds to detect imbalance.
- Define a state machine (`steady`, `investigating`, `draining`, `planPending`, `executing`, `completed`, `failed`) with guardrails (max moves, cooldowns, healthy replica requirements).
- Expose plan APIs (`/meshkit/rebalance-plans` REST + gRPC) for listing, approving, cancelling, monitoring progress.
- Emit events/metrics for controller state changes, plan execution, and guardrail violations.
- Provide CLI verbs (`mesh shards rebalance plan|approve|cancel|watch`) using the shared client helpers.

## Requirements
1. **Policy awareness** – Namespace/cluster policies specify thresholds, concurrent moves, and approvals; they are validated at plan creation.
2. **Safety** – Controller throttles rebalances on incidents, requires at least one healthy replica, and records change-ticket metadata.
3. **Audit** – Persist every plan with before/after snapshots, actor/reason, outcome, and streaming updates.
4. **Transport** – APIs run over HTTP/3/gRPC via OmniRelay builders with downgrade telemetry + RBAC enforcement.
5. **Extensibility** – Allow pluggable scoring adapters (latency, backlog, error rate) via DI so third parties can extend the controller.

## Deliverables
- MeshKit.Rebalancer service + policy/config schema.
- REST/gRPC endpoints, documentation, and OpenAPI/Proto definitions.
- CLI workflows and demo policies showing dry-run, approval, execution, and rollback flows.
- Metrics/alerts integrated with MeshKit observability (feeds WORK-012/015).

## Acceptance Criteria
- Rebalancer schedules dry-run plans, enforces approvals, executes moves, and emits telemetry/alerts per spec.
- CLI/automation invoke the APIs end-to-end with RBAC enforcement and descriptive errors.
- Guardrails prevent over-concentration, respect cooldowns, and expose clear diagnostics when throttling occurs.
- Native AOT publish + tests succeed per WORK-002..WORK-005.

## Testing Strategy
All tiers run against native AOT builds.

### Unit Tests
- Scoring engine combinations (latency/backlog/error), guardrails, cooldown timers, and approval requirements.
- Plan audit builders, serialization, and policy validators.

### Integration Tests
- Controller hosted against MeshKit.Shards + telemetry adapters validating plan lifecycle, RBAC, transport negotiation, and failover behavior.
- CLI automation hitting HTTP/3 + HTTP/2 fallback verifying streaming updates and exit codes.

### Feature Tests
- Feature harness: operator workflow planning, approving, executing, and cancelling plans; verify dashboards/alerts.

### Hyperscale Tests
- Hyperscale harness: simulated fleets with heavy load to ensure guardrails cap concurrent moves and telemetry noise remains manageable.


## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`