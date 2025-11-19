# MeshKit Cluster Descriptor Module

## Purpose
MeshKit.ClusterDescriptors defines geo/region metadata, states, priorities, and governance required for multi-cluster routing and failover. It feeds MeshKit.CrossClusterFailover and operator tooling with authoritative topology data.

## Backlog Reference
- [WORK-015](../../project-board/WORK-015.md) – Descriptor schema + APIs

## Key Touchpoints
- Persists descriptor schema (`clusterId`, region, state, priority, failover policy, annotations, owners) with audit history.
- Extends MeshKit.Registry surfaces so CLI/dashboards can list/filter descriptors.
- Provides readiness signals consumed by failover automation (WORK-016) and operator runbooks (WORK-017, WORK-018).
