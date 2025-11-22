# WORK-009B – Watch Lifecycle (Backoff/Resume/State Machine)

## Goal
Provide reusable watch lifecycle with backoff, resume tokens, and health state machine.

## Scope
- Backoff policy implementation; resume token persistence.
- State machine (Connecting/Staging/Active/Degraded) exposed via metrics/admin.

## Acceptance Criteria
- Forced disconnect tests show correct state transitions and resume without missed epochs.

## Status
Open
