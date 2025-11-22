# WORK-006D – Error & Observability Semantics

## Goal
Standardize errors, status codes, and observability for control streams.

## Scope
- Error taxonomy with remediation hints.
- Metrics/logs/traces for stream state, lag, rejections, capability mismatches.
- Admin/CLI surfacing of control-stream health.

## Acceptance Criteria
- Errors classified and documented; emitted consistently.
- Dashboards/metrics show stream health; tests assert expected signals on induced faults.

## Status
Open
