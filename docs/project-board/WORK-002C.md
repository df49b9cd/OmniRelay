# WORK-002C – CI Perf/SLO Gating

## Goal
Enforce perf SLOs in CI so regressions block merges.

## Scope
- Add CI job to run perf smokes from WORK-002B.
- Define thresholds per mode; fail builds on regression beyond tolerance.
- Surface results in PR checks and artifacts.

## Deliverables
- CI config/scripts.
- Documentation on how to rerun perf gate locally.

## Acceptance Criteria
- Perf gate runs on PRs touching relevant code; fails on regression.
- Nightly full perf run produces trend reports.

## Status
Open

## Testing Strategy
- Unit: Cover new logic/config parsing/helpers introduced by this item.
- Integration: Exercise end-to-end behavior via test fixtures (hosts/agents/registry) relevant to this item.
- Feature: Scenario-level validation of user-visible workflows touched by this item across supported deployment modes/roles.
- Hyperscale: Run when the change affects runtime/throughput/scale; otherwise note non-applicability with rationale in the PR.
