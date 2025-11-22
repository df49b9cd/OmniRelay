# WORK-011C – Kill Switch for Extensions/Policies

## Goal
Provide remote kill switch to disable an extension or policy quickly.

## Scope
- Control-plane command + propagation via protocol.
- Data-plane behavior: disable/bypass with configured fail-open/closed.
- Audit/logging of activation.

## Acceptance Criteria
- Kill switch tested end-to-end; takes effect within bounded time.
- Audit entry recorded; data-plane respects fail-open/closed choice.

## Status
Open
