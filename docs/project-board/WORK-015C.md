# WORK-015C – Multi-Version Bundles & Staged Rollout Integration

## Goal
Support multiple bundle versions for canary/blue-green and integrate with rollout manager.

## Scope
- Produce/version multiple bundles; route labels for staging.
- Handshake with WORK-011 for stage transitions and rollback.

## Acceptance Criteria
- Canary/stage progression works in integration tests; rollback uses LKG when needed.

## Status
Open
