# WORK-001C – Cross-Mode Feature & Performance Validation

## Goal
Validate routing/policy/filter behavior and latency/throughput SLOs across in-proc, sidecar, and edge hosts.

## Scope
- Build feature test matrix to run the same scenarios against each host.
- Add perf smoke benchmarks with p95/p99 targets per mode; record results in CI artifacts.
- Document observed deltas and guidance.

## Deliverables
- Feature test suite updates + perf smoke jobs.
- CI publishing of per-mode perf summaries.

## Acceptance Criteria
- Feature tests pass across all modes with no functional drift.
- Perf smokes show p99 within agreed budgets; regressions fail the job.

## Status
Open
