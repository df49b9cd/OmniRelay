# WORK-007 – Chaos & Health Probe Infrastructure

## Goal
Refactor probe scheduling and chaos hooks into a reusable MeshKit Diagnostics package so control-plane services can register probes/faults through shared APIs, reusing the same metrics, authorization, and CLI surfacing as WORK-020/018/019.

## Scope
- Extract probe registries, scheduler, and result aggregation from existing test harnesses into `MeshKit.Diagnostics.Probes`.
- Provide abstractions for health probes (latency checks, dependency calls), chaos experiments (latency injection, packet loss, node restart), and reporting via telemetry + diagnostics endpoints.
- Integrate with OmniRelay transport builders for secure HTTP/3/gRPC control endpoints.
- Document configuration and operator usage.

## Requirements
1. **Probe abstraction** – Support sync/async probes with intervals/timeouts/thresholds; allow tagging by cluster/namespace.
2. **Chaos controls** – Enable/disable experiments per host with RBAC + confirmation tokens; log every activation.
3. **Telemetry** – Emit metrics/logs for probe success/failure, chaos activations, recovery times.
4. **Security** – Restrict chaos endpoints to authorized operators; integrate with diagnostics runtime toggles.
5. **Extensibility** – DI-friendly registration for new probe/chaos types without editing core library.

## Deliverables
- Library, tests, documentation, and sample registration code for MeshKit modules.

## Acceptance Criteria
- MeshKit services register probes/chaos hooks through the shared infrastructure; diagnostics endpoints expose consistent payloads.
- CLI + dashboards display probe/chaos status across hosts.
- Native AOT publishing/trimming works per WORK-002..WORK-005.

## Testing Strategy
- Unit: probe scheduling, timeout logic, chaos configuration parsing, telemetry counters.
- Integration: register probes against test endpoints, execute chaos hooks, verify diagnostics endpoints + RBAC.
- Feature: use infrastructure inside MeshKit.FeatureTests to drive probes/chaos scenarios.
- Hyperscale: manage hundreds of probes/faults ensuring scheduling + authorization scale.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`