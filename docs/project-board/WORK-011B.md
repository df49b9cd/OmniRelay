# WORK-011B – Health Gates & Rollback

## Goal
Wire telemetry signals (from WORK-012) into rollout stages to auto-pause/rollback on regression.

## Scope
- Define success/rollback criteria; hook into rollout controller.
- Implement pause/rollback actions; status reporting.

## Acceptance Criteria
- Synthetic regression triggers pause/rollback in integration tests.
- Status reflects gate outcomes; alerts fire on rollback.

## Status
Open
