# WORK-018 – MeshKit Operator Dashboards & Alerts

## Goal
Deliver a comprehensive observability pack (Grafana + Prometheus/OTLP alerts) covering MeshKit modules—gossip, leadership, shards, rebalancer, replication, transport downgrade telemetry—so operators have a unified view across clusters.

## Scope
- Build dashboards with panels for leadership scopes, shard distribution, gossip RTT/suspicion, transport downgrade ratios (fed from OmniRelay metrics), replication lag, and cluster states.
- Provide alert rules (Prometheus) for leader flaps, gossip failures, shard imbalance, replication lag, transport downgrade thresholds, and CLI-driven drain/upgrade workflows.
- Include dashboard templating for namespace/cluster filters and RBAC-friendly views.
- Document each dashboard with README entries describing panels, queries, and remediation steps.

## Requirements
1. **Data sources** – Use standardized MeshKit + OmniRelay metrics; keep label cardinality controlled.
2. **Docs** – Each dashboard must include instructions, screenshots, and CLI tie-ins.
3. **Testing** – Add automated provisioning tests/screenshot diffs, Prometheus rule unit tests, and synthetic checks verifying dashboards stay healthy after deploys.
4. **Versioning** – Store JSON/YAML under version control with release notes.
5. **AOT** – Exporters/instrumentation must function in native AOT hosts.

## Deliverables
- Grafana dashboards, Prometheus rule files, synthetic check definitions.
- Documentation linking dashboards to runbooks/CLI commands.

## Acceptance Criteria
- Dashboards visualize data in staging; SREs approve.
- Alerts trigger during simulated failures and respect silences/maintenance windows.
- Synthetic checks run post-deploy ensuring dashboards + alerts stay healthy.

## Testing Strategy
- Unit: JSON/YAML linting, derived metric calculations.
- Integration: Provision dashboards/rules against staging data, capture screenshot diffs.
- Feature: Run operator game days injecting various failures, verifying dashboards/alerts guide response.
- Hyperscale: Stress dashboards with simultaneous incidents across clusters, ensuring dedup + silence handling works.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`