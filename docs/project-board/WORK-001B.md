# WORK-001B – Admin/Diagnostics Parity Across Modes

## Goal
Expose consistent admin/diagnostic surfaces (mode, epoch, filter chain, capabilities) for in-proc, sidecar, and edge hosts.

## Scope
- Align admin endpoints/metrics emitted per mode.
- Ensure mode metadata (mode, capability set, build epoch) is exposed uniformly.
- Add smoke tests that query admin endpoints for each host flavor.

## Deliverables
- Admin endpoint updates and metrics additions.
- Docs: “Admin surfaces by deployment mode.”

## Acceptance Criteria
- Admin endpoints return comparable payloads across modes; differences documented.
- Integration tests query admin endpoints for all modes and pass.
- AOT publish green for modified hosts.

## Status
Open
