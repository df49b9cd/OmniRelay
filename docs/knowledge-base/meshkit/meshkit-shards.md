# MeshKit Shards Module

## Purpose
MeshKit.Shards is the control-plane service responsible for surfacing shard ownership data, simulations, and diff streams over HTTP/3 + gRPC. It provides the data model that MeshKit.Rebalancer, MeshKit.Registry clients, and the OmniRelay CLI consume.

## Backlog Reference
- [WORK-010](../../project-board/WORK-010.md) – APIs & Tooling
- [WORK-013](../../project-board/WORK-013.md) – Registry Read APIs (shard snapshots)
- [WORK-014](../../project-board/WORK-014.md) – Registry Mutation APIs

## Key Touchpoints
- Exposes `/meshkit/shards` endpoints and watch streams.
- Drives CLI verbs such as `omnirelay mesh shards list|diff|simulate`.
- Persists shard state via the shared stores (`OmniRelay.ShardStore.*`) with optimistic concurrency.
- Emits telemetry consumed by dashboards (WORK-012) and operator alerts (WORK-018).
