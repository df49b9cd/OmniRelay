# WORK-013 – MeshKit.Registry Read APIs

## Goal
Expose authoritative registry data (peers, clusters, versions, config) via MeshKit-hosted HTTP/3 and gRPC endpoints so operators, dashboards, and automation have a single control surface for discovery without touching OmniRelay internals.

## Scope
- Implement `GET /meshkit/peers`, `/meshkit/clusters`, `/meshkit/versions`, `/meshkit/config` with filtering, pagination, sorting, and ETag caching.
- Provide SSE/gRPC streaming endpoints for peer/cluster updates with resume tokens and negotiated transport metadata.
- Generate OpenAPI/Protobuf contracts and sample queries; update CLI commands (`mesh peers *`, `mesh clusters list`, etc.) to consume the MeshKit APIs.

## Requirements
1. **RBAC** – Enforce scopes (`mesh.read`, `mesh.observe`) and record audit events for unauthorized attempts.
2. **Performance** – P95 <200 ms for 1k peers; streaming endpoints must sustain 100+ subscribers.
3. **Caching** – ETag + `If-None-Match` semantics with consistent version metadata tied to MeshKit.Shards registry revisions.
4. **Consistency** – Document snapshot semantics (last committed registry version) and ensure CLI/dashboards align.
5. **Observability** – Log negotiated protocol/encoding, emit metrics for request latency + stream subscribers, and expose downgrade counters.

## Deliverables
- MeshKit.Registry read service + documentation.
- CLI/SDK updates to rely solely on MeshKit endpoints.
- Integration tests + sample scripts for dashboards and automation.

## Acceptance Criteria
- Endpoints return accurate data, enforce RBAC, and power CLI watchers (`--watch`) with resume tokens.
- Unauthorized access yields 401/403 responses and audit logs.
- CLI output and dashboards stay consistent with MeshKit snapshots even during downgrades or reconnects.
- Native AOT publish/tests succeed (WORK-002..WORK-005).

## Testing Strategy
- Unit tests for filter/pagination/ETag validators and stream fan-out handling.
- Integration tests hitting HTTP/3 + forced HTTP/2, verifying RBAC, caching, and streaming.
- Feature tests simulating peer churn with CLI + dashboards verifying consistent data.
- Hyperscale tests stressing streaming clients and RBAC churn.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`