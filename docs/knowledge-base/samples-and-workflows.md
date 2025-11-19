# Samples & Workflows

## Samples Directory
- `samples/Configuration.Server` – configuration-first hosting example; demonstrates `AddOmniRelayDispatcher` with multiple inbounds/outbounds and control-plane toggles.
- `samples/DistributedDemo/*` – multi-service scenario (Gateway, Inventory, Audit, Shared) showing peer choosers, codec diversity, and cross-process RPC.
- `samples/Observability.CliPlayground` – pairs CLI commands with dispatcher hosts to demo diagnostics flows.
- `samples/ResourceLease.MeshDemo` – exercises SafeTaskQueue + lease orchestrations.
- `samples/Shadowing.Server`, `samples/CodegenTee.Rollout`, `samples/HybridRunner`, etc., highlight tee/shadow strategies, codegen usage, and multi-inbound deployments.

## Runbooks & Docs
- `docs/reference/diagnostics.md` – operator guide for control-plane endpoints, CLI shard commands, drain/resume flows, and Prometheus scraping.
- `docs/architecture/omnirelay-rpc-mesh.md` – architecture for resource leases, peer choosers, and mesh observability.
- `docs/project-board/WORK-*.md` – active feature briefs (transport-hardening items WORK-001..005 and MeshKit modules WORK-006..022) capturing goals, scope, acceptance criteria, and test expectations.

## Operator Workflows
1. **Validate configuration**: `omnirelay config validate --config appsettings.json`.
2. **Run dispatcher locally**: `dotnet run --project src/OmniRelay.Cli -- serve --config ...` or host via Generic Host.
3. **Monitor**: Use `/omnirelay/introspect`, `omnirelay mesh peers`, and shard commands for visibility.
4. **Perform drain/upgrade**: `omnirelay mesh upgrade drain/resume` hits `/control/upgrade` endpoints while CLI renders progress.
5. **Simulate sharding changes**: `omnirelay mesh shards simulate --namespace foo --node node-a:1 --node node-b:0.8` to preview rebalance plans.

These samples plus the CLI/workflow docs provide end-to-end references for dispatcher authors, SREs, and feature teams.
