# Project Board Overview

This file lists dependency and parallelization guidance for the DISC-### implementation cards.

## Dependencies

- Foundation: `DISC-001` (Gossip) and `DISC-002` (Leadership) must land before routing metadata, rebalancer, or multi-cluster work begins.
- Registry data: `DISC-003` (Shard schema) precedes `DISC-004` (Shard APIs), `DISC-005` (Rebalancer), and `DISC-013` (Transport policy checks).
- Security: `DISC-009` (Bootstrap) is prerequisite for `DISC-010` (Join tooling), `DISC-013` (Transport governance), and any environment scripts that require mTLS.
- Multi-cluster: `DISC-011` (Cluster descriptors) must complete before `DISC-012` (Replication/failover).
- Observability consumers (DISC-015/017) rely on telemetry emitted by DISC-002/004/005/012.

## Parallelizable Workstreams

- Once `DISC-001`/`002` are underway, teams can begin `DISC-003` (schema) and `DISC-009` (security) in parallel.
- After `DISC-003` is available, `DISC-004`, `DISC-005`, and `DISC-013` can proceed concurrently (with coordination on shared contracts).
- `DISC-006` (Rebalance observability) can build alongside `DISC-005` once metrics are defined.
- `DISC-007` (Registry read APIs) can start as soon as base registry storage is ready; `DISC-008` follows immediately after read path stabilization.
- `DISC-010` (Join tooling) follows `DISC-009` but does not block other control-plane efforts.
- `DISC-012` (Replication/failover) may start once `DISC-011` is done and leadership is stable (`DISC-002`).
- `DISC-014`/`015` (docs + dashboards) can iterate in parallel once upstream APIs/metrics stabilize.
- `DISC-016` (CLI) can track behind the APIs it wraps (`DISC-004`, `DISC-007`, `DISC-008`, `DISC-012`).
- `DISC-017` (synthetic checks) and `DISC-018` (chaos environments) can begin after minimum viable control plane is available (`DISC-001`-`004`).
- `DISC-019` (chaos automation) sits on top of `DISC-018` and matures as more features land.

Refer back to `docs/architecture/service-discovery.md` for detailed requirements per story.

## Epic Structure & Subtasks

- **E1 - Service Discovery & Transport (DISC-001)**: `REFDISC-001`, `REFDISC-002`, `REFDISC-003`, `REFDISC-005`, `REFDISC-006`, `REFDISC-007`, `REFDISC-008`, `REFDISC-013`, `REFDISC-020`, `REFDISC-032`.
- **E2 - Leadership & Coordination (DISC-002)**: `REFDISC-015`, `REFDISC-011`, `REFDISC-033`.
- **E3 - Registry & Persistence (DISC-003`-`DISC-008)**: `REFDISC-016`, `REFDISC-019`, `REFDISC-021`, `REFDISC-022`, `REFDISC-031`.
- **E4 - Security & Bootstrap (DISC-009`-`DISC-013)**: `REFDISC-003`, `REFDISC-014`, `REFDISC-023`, `REFDISC-026`, `REFDISC-025`.
- **E5 - Diagnostics & Control Plane UX (DISC-014`-`DISC-018)**: `REFDISC-004`, `REFDISC-009`, `REFDISC-010`, `REFDISC-013`, `REFDISC-024`, `REFDISC-027`, `REFDISC-028`, `REFDISC-017`, `REFDISC-018`.
- **E6 - Chaos & Reliability (DISC-017`-`DISC-019)**: `REFDISC-017`, `REFDISC-018`, `REFDISC-020`, `REFDISC-021`.
- **E7 - Platform, Tooling & AOT (cross-cutting)**: `REFDISC-029`, `REFDISC-030`, `REFDISC-034`, `REFDISC-035`, `REFDISC-036`, `REFDISC-037`.

Use these epics to track dependencies: child REFDISC cards should not close until their parent DISC epic’s acceptance criteria are met.

## AOT-First Requirement

OmniRelay is cloud-native and hyperscale-focused, so **every DISC/REFDISC story carries an explicit AOT gate**:
- Native AOT publishes (`dotnet publish /p:PublishAot=true`) must succeed with trimming warnings treated as errors for the assemblies touched by the story.
- Unit/integration/feature/hyperscale tests must run against the native artifacts at least once before closure.
- CI enforcement lives in `REFDISC-034`-`REFDISC-037`; reference those cards in your deliverables.

Stories lacking the “Native AOT gate” acceptance bullet or the “All test tiers must run against native AOT artifacts” note should be updated before work begins.
