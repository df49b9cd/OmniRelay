# REFDISC-037 - AOT CI Gating & Runtime Validation

## Goal
Bake native AOT build/test runs into the CI pipeline so every commit proves AOT readiness for dispatcher, control-plane services, and tooling, preventing regressions as the codebase evolves.

## Scope
- Add CI jobs that run `dotnet publish /p:PublishAot=true` for dispatcher, key control-plane hosts, and CLI on representative RIDs.
- Execute smoke/integration tests against the produced binaries (where feasible) inside containers.
- Fail builds when trimming/AOT warnings appear or when runtime smoke tests fail.
- Surface AOT status badges/metrics to developers.

## Requirements
1. **CI coverage** - At minimum, linux-x64 AOT builds for dispatcher, gossip host, leadership host, and CLI must run per PR; additional RIDs added as optional stages.
2. **Caching** - Optimize pipeline to reuse artifacts and keep build times reasonable (< target threshold).
3. **Test harness** - Include smoke tests (start host, run a few RPCs) to ensure published binaries are functional.
4. **Reporting** - Provide clear CI output for AOT failures (which project, warnings) and integrate with PR checks.
5. **Developer docs** - Document how to run AOT validation locally and interpret CI failures.

## Deliverables
- CI workflow updates (GitHub Actions/Azure DevOps) with AOT build/test jobs.
- Smoke-test scripts for dispatcher/CLI native binaries.
- Documentation describing CI expectations and local reproduction steps.

## Acceptance Criteria
- AOT CI jobs run on each PR and block merges on failure.
- Build time increase stays within agreed budget (track metrics).
- Developers can reproduce CI AOT failures locally using documented commands.
- AOT badge/indicator added to README or dashboards.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Ensure any new scripting/helpers introduced for AOT builds have unit coverage (e.g., script modules).

### Integration tests
- Run automated smoke tests within CI: start AOT dispatcher, issue RPC/gossip requests, confirm success.
- Validate CLI AOT binaries run targeted commands in CI environment.

### Feature tests
- Periodically trigger feature test suites (nightly) against AOT builds to ensure broader coverage beyond smoke tests.

### Hyperscale Feature Tests
- Schedule hyperscale suites (weekly) using AOT binaries to ensure long-running/regression scenarios stay healthy; track metrics for startup time improvements.

## References
- REFDISC-034/035/036 deliverables (projects to build), CI infrastructure docs.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
