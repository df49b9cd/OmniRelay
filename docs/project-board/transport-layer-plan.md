# Transport Layer Realignment Plan

This plan sequences the WORK-xxx items that keep OmniRelay transport-focused, extract MeshKit control-plane modules, and enforce native AOT at every layer. Use it with `docs/architecture/transport-layer-vision.md` and the updated project-board README.

## Phase 0 – Alignment & Governance (Week 0-1)
- Publish the transport-layer vision and communicate ownership boundaries across teams.
- Tag backlog items with their owning layer (Hugo, OmniRelay, MeshKit) and freeze new OmniRelay work that introduces control-plane state.
- Establish a tri-layer design review (Hugo + OmniRelay + MeshKit) that blocks transport stories lacking MeshKit/Hugo dependency notes.

## Phase 1 – Transport Hardening (Weeks 1-4)
- Complete WORK-001 → WORK-005 (transport policy gate + OmniRelay/MeshKit/CLI AOT baseline, packaging, and CI gating).
- Ensure CLI verbs already implemented (`leaders`, `peers`, etc.) rely on MeshKit endpoints exclusively and surface downgrade telemetry.
- Keep `dotnet build OmniRelay.slnx`, `dotnet test` (unit/integration/feature/hyperscale), and native AOT publishes in every PR gate.

## Phase 2 – MeshKit Core Surfaces (Weeks 3-6)
- Deliver WORK-006 → WORK-009 (client helpers, chaos/probe infrastructure, config watchers, bootstrap harness) so future MeshKit modules reuse the same transport/auth runtime.
- Wire shared kits into samples/FeatureTests so MeshKit extraction work has reliable fixtures.

## Phase 3 – MeshKit Registry & Shards (Weeks 5-9)
- Implement WORK-010 → WORK-014: MeshKit.Shards, MeshKit.Rebalancer, Rebalance observability, registry read/mutation APIs.
- Update CLI (WORK-019) alongside these modules so operators exercise MeshKit endpoints only.

## Phase 4 – Multi-Cluster & Failover (Weeks 8-12)
- Build WORK-015 (MeshKit.ClusterDescriptors) and WORK-016 (Cross-Cluster Failover) once registry/rebalancer modules are healthy.
- Validate planned/emergency failovers via integration + hyperscale suites using MeshKit automation.

## Phase 5 – Operator Experience & Reliability (Weeks 9-13)
- Refresh docs/samples (WORK-017), dashboards/alerts (WORK-018), CLI UX (WORK-019), synthetic probes (WORK-020), chaos environments (WORK-021), and automated chaos gating (WORK-022).
- Ensure dashboards/alerts/probes/chaos outputs all depend on MeshKit metrics rather than OmniRelay internals.

## Validation Checklist
- **Build/Test**: `dotnet build OmniRelay.slnx`, `dotnet test` across core suites, plus MeshKit-specific test projects as they split.
- **Native AOT**: `dotnet publish -r linux-x64 -c Release /p:PublishAot=true` for OmniRelay dispatcher, MeshKit hosts, and CLI (WORK-002..WORK-005).
- **Docs/Samples**: Update knowledge base, samples, and onboarding docs any time WORK-017 changes behavior.
- **Observability**: Dashboards/alerts (WORK-012, WORK-018) must highlight MeshKit signals; OmniRelay dashboards focus on transport health/downgrades.

## Exit Criteria
- OmniRelay repo contains only transport, middleware, diagnostics, and CLI logic; MeshKit repositories own gossip, leadership, shards, rebalancer, replication, bootstrap, and chaos workflows.
- CLI bundles default to MeshKit endpoints for control-plane operations with transport diagnostics kept separate.
- Native AOT CI gating (WORK-005) remains green, and hyperscale suites cover both transport + control-plane interplay using the layered deployment model.
