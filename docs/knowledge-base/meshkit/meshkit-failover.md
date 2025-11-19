# MeshKit Cross-Cluster Failover Module

## Purpose
MeshKit.CrossClusterFailover coordinates planned and emergency promotions/demotions across clusters using replication logs, version vectors, and the descriptor metadata defined in WORK-015. It ensures routing metadata stays consistent during regional outages.

## Backlog Reference
- [WORK-016](../../project-board/WORK-016.md) – Replication + failover workflows

## Key Touchpoints
- Publishes replication lag metrics (`mesh_replication_*`) and failover workflow status consumed by dashboards (WORK-018).
- Provides CLI flows `omnirelay mesh clusters promote|failback|replication status` for operators.
- Integrates with MeshKit.Rebalancer outputs to ensure shard ownership aligns post-failover.
- Emits audit logs and alerts that chaos automation (WORK-021/022) relies on during drills.
