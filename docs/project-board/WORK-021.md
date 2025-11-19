# WORK-021 – MeshKit Chaos Environments

## Goal
Provision reproducible environments (docker-compose + Kubernetes) for executing scripted chaos experiments targeting MeshKit modules (gossip, leadership, shards, rebalancer, replication) while measuring impact through OmniRelay transports and MeshKit observability.

## Scope
- Create compose stack launching MeshKit modules, OmniRelay transport endpoints, Prometheus/Grafana, and fault-injection helpers (tc/netem, chaos containers).
- Provide Kubernetes manifests/Helm charts replicating setup with configurable node counts and namespaces.
- Author scripts to trigger faults: kill leaders, partition network segments, inject latency/packet loss, surge join/leave events, simulate cert expiry, force transport downgrades.
- Collect artifacts (logs, metrics, topology snapshots) for analysis.

## Requirements
1. **Determinism** – One-command setup with documented prerequisites; environment resets cleanly.
2. **Fault library** – Parameterized faults (duration, intensity) with ability to chain scenarios.
3. **Telemetry** – Observability stack included by default; ensures WORK-018 dashboards render inside chaos env.
4. **Cleanup** – Provide teardown scripts; ensure secrets/certs rotated per run.
5. **AOT** – Support running native AOT binaries inside the chaos env.

## Deliverables
- Docker-compose + Kubernetes assets, helper scripts, documentation, sample scenarios.

## Acceptance Criteria
- Engineers start the chaos environment locally/in cluster, execute sample scenarios, and collect artifacts per documentation.
- Observability dashboards/alerts operate inside the chaos environment.
- Fault catalog covers leader kill, network partition, latency injection, membership surge, transport downgrade, cert expiration.

## Testing Strategy
- Unit: schema tests for scenario definitions, script linting, determinism checks.
- Integration: CI jobs boot chaos env, run sample faults, capture artifacts.
- Feature: run documented chaos scenarios as game days, ensuring cleanup resets environment.
- Hyperscale: execute chained faults across large node counts verifying orchestration reliability and observability at scale.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`