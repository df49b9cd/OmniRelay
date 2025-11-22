# WORK-003D – Extension Telemetry & Failure Policy Wiring

## Goal
Provide unified telemetry and failure policy handling across DSL, Wasm, and native hosts.

## Scope
- Metrics/logs for load, instantiation, execution latency, watchdog trips, failures.
- Configurable fail-open/closed/reload per extension.
- Admin endpoint summarizing extension state.

## Acceptance Criteria
- Telemetry emitted consistently for all extension types.
- Failure policies enforce correctly in integration/chaos tests.
- Admin endpoint shows per-extension status and last error.

## Status
Open
