# WORK-014 – MeshKit.Registry Mutation APIs

## Goal
Provide authenticated MeshKit control-plane verbs for mutating registry state (cordon, drain, label, promote/demote clusters, edit config) so operators and automation can manage mesh topology without embedding OmniRelay-specific logic.

## Scope
- Add mutation endpoints:
  - `POST /meshkit/peers/{id}:cordon|uncordon`
  - `POST /meshkit/peers/{id}:drain`
  - `PATCH /meshkit/peers/{id}` for metadata/labels
  - `POST /meshkit/clusters/{id}:promote|demote`
  - `PATCH /meshkit/config`
- Enforce optimistic concurrency (etag/version) and record audit events (actor, ticket, reason, result).
- Wire CLI verbs (`mesh peers drain`, `mesh clusters promote`, etc.) with confirmation prompts, dry-run, and JSON/table output.

## Requirements
1. **RBAC & safety** – Require `mesh.operate`/`mesh.admin` scopes, double-confirm destructive actions, validate preconditions (e.g., cannot demote last active cluster).
2. **Idempotency** – Mutations must be idempotent; retries with stale etags yield clear errors.
3. **Telemetry** – Log/metric operation type, result, latency, negotiated transport; expose metrics for operations/sec and failures.
4. **Auditability** – Persist change history referencing ticket IDs and propagate to event bus for observers.
5. **Extensibility** – Mutation pipeline should register domain handlers via DI, allowing new workflows without modifying base controller.

## Deliverables
- MeshKit mutation controllers + domain handlers.
- CLI workflows + integration tests covering success/failure cases.
- Docs + runbooks describing guardrails and rollback steps.

## Acceptance Criteria
- Registry state reflects mutations, RBAC is enforced, and audit trails capture all metadata.
- CLI commands demonstrate cordon/drain/promote flows end-to-end with descriptive prompts/errors.
- Error handling covers invalid states with actionable problem details.
- Native AOT publish/tests succeed per WORK-002..WORK-005.

## Testing Strategy
- Unit tests for validators, etag helpers, audit builders, and CLI prompt logic.
- Integration tests executing REST + gRPC mutations under parallel load, verifying RBAC + audit persistence.
- Feature tests running scripted operations (cordon/drain/promote) validating watchers/dashboards update immediately.
- Hyperscale tests stressing concurrent mutations and ensuring optimistic concurrency + audit throughput hold.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`