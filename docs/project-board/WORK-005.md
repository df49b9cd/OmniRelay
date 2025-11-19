# WORK-005 – AOT CI Gating & Runtime Validation

## Goal
Integrate native AOT build/test runs into CI/CD so every commit proves OmniRelay transports, MeshKit services, and the CLI remain AOT-ready, preventing regressions as the layered architecture evolves.

## Scope
- Add CI jobs (GitHub Actions/Azure Pipelines) that run `dotnet publish /p:PublishAot=true` for OmniRelay dispatcher, representative MeshKit modules (shards, rebalancer, cluster descriptors), and the CLI for linux-x64 (minimum) plus optional additional RIDs.
- Execute smoke/integration tests against the produced binaries (launch hosts, run CLI commands, execute control-plane RPCs) inside containers.
- Fail builds on trimming/AOT warnings or runtime smoke-test failures; surface clear logs.
- Publish status badges/metrics and document how to reproduce failures locally.

## Requirements
1. **CI coverage** – At minimum, linux-x64 AOT builds for dispatcher, MeshKit.Shards, MeshKit.Rebalancer, MeshKit.ClusterDescriptors, and CLI per PR; optional matrix for other RIDs.
2. **Caching** – Optimize pipeline runtimes with caching of NuGet/dotnet artifacts.
3. **Smoke tests** – Start AOT hosts in CI containers, call key endpoints (peers, shards, rebalancer), and run core CLI commands.
4. **Reporting** – Provide readable CI output (which project, warnings) and add README badges/dashboards.
5. **Developer guidance** – Document local commands to reproduce AOT builds/tests.

## Deliverables
- CI workflow updates, smoke-test scripts, documentation.

## Acceptance Criteria
- CI blocks merges when AOT builds/tests fail; build time impact stays within agreed budget.
- Developers can reproduce failures locally via documented commands.
- Status badges/metrics reflect AOT health.

## Testing Strategy
- Unit: if scripts/analyzers added, ensure coverage.
- Integration: CI job executes publish + smoke tests; manual dry runs documented.
- Feature/Hyperscale: periodic feature/hyperscale suites executed against AOT artifacts to ensure deep coverage beyond smoke tests.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`