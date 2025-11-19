# MeshKit Rebalancer Module

## Purpose
MeshKit.Rebalancer ingests MeshKit.Shards data, peer health signals, and Hugo backpressure metrics to generate safe shard movement plans. It supports dry-run previews, approval workflows, execution tracking, and emits controller telemetry.

## Backlog Reference
- [WORK-011](../../project-board/WORK-011.md) – Controller + policy engine
- [WORK-012](../../project-board/WORK-012.md) – Observability package (dashboards/alerts)

## Key Touchpoints
- Consumes MeshKit.Shards snapshots and peer health feeds.
- Hosts `/meshkit/rebalance-plans` REST/gRPC surfaces for plan listing, approvals, and execution monitoring.
- Powers CLI commands `omnirelay mesh shards rebalance *` and operator runbooks referenced in WORK-011/012.
- Supplies metrics (`meshkit_rebalance_*`) to dashboards/alerts defined in WORK-012 and to synthetic probes/chaos testing (WORK-020/021).
