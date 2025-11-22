# WORK-003C – Native Plugin ABI & Watchdogs

## Goal
Enable loading native plugins via `NativeLibrary.Load` with a stable function-pointer ABI and watchdog enforcement.

## Scope
- Define ABI (function table, version handshake) and sample plugin.
- Signature/manifest verification reused from registry.
- Watchdog/timeouts and failure policy for native calls.

## Acceptance Criteria
- Sample native plugin loads/runs; incompatible ABI rejected with clear error.
- Watchdog triggers terminate/skip per policy; telemetry emitted.

## Status
Open
