# WORK-012 – MeshKit.Rebalance Observability Package

## Goal
Bundle dashboards, alerts, and documentation that make MeshKit.Rebalancer activity observable across clusters in real time, ensuring operators can reason about shard movement without touching OmniRelay internals.

## Scope
- Instrument MeshKit.Rebalancer with Prometheus metrics (`meshkit_rebalance_state`, `meshkit_rebalance_shards_in_flight`, `meshkit_rebalance_duration_seconds`, `meshkit_rebalance_plan_approvals_total`).
- Produce Grafana dashboards (exec + on-call views) templated by namespace/cluster showing plan queues, per-node shard counts, drain timelines, and approval backlog.
- Define alert rules for stuck plans, excessive concurrent moves, repeated failures, missing controller heartbeats, and policy violations.
- Document runbooks linking dashboards, CLI commands, and remediation workflows.

## Requirements
1. **MeshKit data sources** – Metrics flow from MeshKit.Rebalancer and MeshKit.Shards; OmniRelay transports remain unchanged.
2. **Dashboard governance** – JSON kept under version control with linting/tests; include screenshot diffs or storybook snapshots.
3. **Alert routing** – Provide Prometheus rules + sample PagerDuty/Teams integrations with templated annotations.
4. **Docs** – Update `docs/knowledge-base` + operator guides with setup instructions, screenshots, and CLI tie-ins.
5. **AOT gate** – Observability exporters must work in native AOT builds per WORK-002..WORK-005.

## Deliverables
- Metrics wiring + unit tests for label cardinality.
- Grafana dashboards, Prometheus rule files, screenshot artifacts, and provisioning instructions.
- Runbook markdown referencing CLI workflows (`mesh shards rebalance ...`).

## Acceptance Criteria
- Dashboards render against staging MeshKit deployments with healthy + failing plan scenarios.
- Alerts fire for simulated incidents and stay quiet during steady state.
- Documentation reviewed by SRE + product stakeholders with validated walkthroughs.
- Native AOT tests + linting for dashboards/rules succeed in CI.

## Testing Strategy
- Unit tests for metrics labels + JSON/YAML schema validation.
- Integration tests provisioning dashboards/rules against sample data (Grafana provisioning tests, Prometheus rule unit tests).
- Feature tests: run rebalancer scenarios inside feature harness verifying dashboards/alerts/runbooks.
- Hyperscale tests: stress dashboards with large plan counts and ensure alert volume manageable.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`
- `docs/knowledge-base/shards-overview.md`