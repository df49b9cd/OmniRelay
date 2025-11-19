# WORK-020 – MeshKit Synthetic Health Checks

## Goal
Deploy MeshKit synthetic probes that continuously validate control-plane functionality (MeshKit APIs, streams, HTTP/3 negotiation) using least-privilege credentials, providing early detection of regressions without depending on production traffic.

## Scope
- Build probe agents (container/cron job) that periodically:
  - Call `/meshkit/peers`, `/meshkit/shards`, `/meshkit/clusters`, `/meshkit/versions` and validate responses, latency, and auth behavior.
  - Subscribe to leadership/shard watch streams ensuring heartbeat cadence and resume tokens function.
  - Execute gRPC calls over HTTP/3, forcing downgrades to HTTP/2 to confirm telemetry + CLI `transport stats` alignment.
- Feed results into Prometheus/Grafana and alerting pipelines.

## Requirements
1. **Configurable targets** – Support per-cluster/namespace schedules, thresholds, and probe types via declarative config.
2. **Isolation** – Probes run with read-only scopes (`mesh.observe`) and independent networking so they don’t depend on production paths.
3. **Alerting** – Consecutive failures or latency breaches trigger alerts via the shared MeshKit alerting framework.
4. **Reporting** – Generate daily/weekly health reports summarizing success rates/global latency trends.
5. **AOT** – Probe binaries publish as native AOT per WORK-002..WORK-005.

## Deliverables
- Probe service/container + Helm chart/manifest.
- Metrics/alerts dashboards for probe status.
- Documentation for deployment, tuning, troubleshooting.

## Acceptance Criteria
- Synthetic checks detect injected failures (API downtime, slow responses, stalled streams) and alert within configured windows.
- Dashboards/reporting show pass/fail trends; probes adjustable via config reload.
- Native AOT builds/tests succeed.

## Testing Strategy
- Unit: config parsing, scheduler, timeout handling, classification/report builders.
- Integration: deploy probe against staging cluster verifying API calls, stream subscriptions, HTTP/3 fallback, and alert triggers.
- Feature: incident rehearsals where probes detect outages and responders follow documented steps.
- Hyperscale: fleets of probes per cluster/namespace ensuring coordination and alert fan-out scale.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`
