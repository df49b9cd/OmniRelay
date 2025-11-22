# WORK-009C – Validation Hooks & Observability

## Goal
Add schema/capability/extension signature validation during startup/watch, with observability.

## Scope
- Validation pipeline hooks; blocking activation on errors.
- Metrics/logs for validation failures and successful activations.

## Acceptance Criteria
- Invalid configs/extensions are rejected with actionable errors; metrics emitted.
- Feature tests cover validation failure paths.

## Status
Open
