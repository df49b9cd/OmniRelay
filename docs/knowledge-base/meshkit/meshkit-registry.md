# MeshKit Registry Module

## Purpose
MeshKit.Registry exposes versioned read/write APIs for peers, clusters, versions, and configuration. It fills the role of the control-plane data source that dashboards, CLI commands, and automation rely upon.

## Backlog Reference
- [WORK-013](../../project-board/WORK-013.md) – Read APIs for peers/clusters/config
- [WORK-014](../../project-board/WORK-014.md) – Mutation APIs (cordon/drain/promote/config edits)

## Key Touchpoints
- HTTP/3 + gRPC endpoints under `/meshkit/peers`, `/meshkit/clusters`, `/meshkit/versions`, `/meshkit/config`, and corresponding mutation verbs.
- Streams updates to watchers (CLI `--watch`, dashboards, synthetic probes) with resume tokens and downgrade telemetry.
- Enforces RBAC scopes (`mesh.read`, `mesh.observe`, `mesh.operate`, `mesh.admin`) and optimistic concurrency via ETags.
- Emits audit events accessible to observability tooling (WORK-012, WORK-018) and chaos automation (WORK-022).
