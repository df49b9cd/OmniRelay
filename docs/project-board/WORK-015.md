# WORK-015 – MeshKit.Cluster Descriptors

## Goal
Create MeshKit.ClusterDescriptors as first-class registry entities capturing geo metadata, state, priorities, failover policy, and governance so multi-region routing can be automated independently of OmniRelay transports.

## Scope
- Define descriptor schema (`clusterId`, `region`, `state`, `priority`, `failoverPolicy`, `replicationEndpoints`, `owners`, `annotations`, `changeTicket`).
- Persist descriptors in MeshKit registry with versioning/audit history.
- Extend MeshKit.Registry read APIs/CLI to list/filter descriptors; expose watchers for topology changes.
- Validate transitions (active ↔ passive ↔ draining ↔ maintenance) and priority rules.

## Requirements
1. **Governance fields** – Require owner/team metadata, change-ticket references, and annotations for compliance.
2. **Failover metadata** – Track planned vs emergency flags, dependencies, and readiness indicators consumed by WORK-016.
3. **Observability** – Metrics for cluster counts per state, failover readiness, and descriptor drift.
4. **RBAC** – Enforce `mesh.operate` for updates; read APIs remain `mesh.read`.
5. **Documentation** – Provide lifecycle guidance and CLI/automation examples.

## Deliverables
- Descriptor schema + migrations.
- APIs/CLI updates and documentation/runbooks.
- Metrics/dashboards showing topology state.

## Acceptance Criteria
- Operators create/update descriptors via API/CLI with validation and audit logs.
- MeshKit.ClusterDescriptors watchers/dashboards reflect changes immediately and feed failover automation.
- Native AOT publish/tests succeed (WORK-002..WORK-005).

## Testing Strategy
- Unit tests for schema validation, transition rules, and diff/audit builders.
- Integration tests executing CRUD via REST/gRPC, verifying RBAC + optimistic concurrency + watcher updates.
- Feature tests simulating geo deployments and verifying dashboards/alerts.
- Hyperscale tests managing many clusters with concurrent edits ensuring conflict detection + observability scale.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`