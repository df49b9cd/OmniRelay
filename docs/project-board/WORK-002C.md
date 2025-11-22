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
