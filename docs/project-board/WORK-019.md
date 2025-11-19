# WORK-019 – OmniRelay Mesh CLI Enhancements

## Goal
Evolve the `omnirelay mesh` CLI into the canonical operator surface for MeshKit modules (shards, clusters, rebalancer, failover, transport policy) while maintaining native AOT readiness and transport-only responsibilities within OmniRelay.

## Scope
- Implement subcommands:
  - `peers list/status/drain/cordon`
  - `leaders status --watch`
  - `shards list/diff/simulate/rebalance`
  - `clusters list/promote/failback`
  - `config validate/show`
  - `transport stats`
  - `debug downgrade-events`
- Support JSON + table output, filtering/pagination, interactive confirmations, `--watch`, `--dry-run`, and `--scope` flags aligned with MeshKit RBAC.
- Provide completion scripts (bash/zsh/pwsh) and help docs referencing the new architecture.

## Requirements
1. **Auth reuse** – CLI uses MeshKit/OmniRelay shared auth (mTLS, tokens) via REFWORK-020 helpers and surfaces clear errors for expired credentials.
2. **UX consistency** – Commands share option patterns, progress indicators, colorized warnings (when supported), and structured exit codes.
3. **Testing** – Integration tests hitting MeshKit fixtures; golden-file tests for output formatting; CLI analyzers ensuring options remain trimmed/AOT-safe.
4. **Extensibility** – Command architecture allows new verbs/modules without refactoring core plumbing.
5. **AOT** – CLI ships native binaries per WORK-004 and passes AOT smoke tests per WORK-005.

## Deliverables
- CLI implementations, tests, completion scripts, packaging/release notes.
- Documentation + demo scripts for onboarding/training.

## Acceptance Criteria
- CLI workflows exercise MeshKit APIs end-to-end (list shards, simulate, drain peers, promote clusters) with deterministic output.
- `config validate` catches policy violations before deployment.
- Native AOT build/publish for Linux/macOS/Windows is part of CI.

## Testing Strategy
- Unit tests for command routing, option binding, prompts, output format.
- Integration tests hitting MeshKit fixtures (HTTP/3 + forced HTTP/2) verifying RBAC, streaming, destructive flows.
- Feature tests scripting operator workflows solely via CLI.
- Hyperscale tests: CLI handles pagination through tens of thousands of objects and sustained watch sessions.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`