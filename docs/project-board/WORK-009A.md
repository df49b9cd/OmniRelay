# WORK-009A – Deterministic Startup Pipeline

## Goal
Implement shared startup flow: load/validate LKG, fetch latest snapshot, stage, activate.

## Scope
- Order of operations; failure handling; rollback to safe state.
- Logging of each phase.

## Acceptance Criteria
- Startup follows defined order; failures leave system in non-active safe state.
- Integration tests cover good/bad configs and LKG fallback.

## Status
Open
