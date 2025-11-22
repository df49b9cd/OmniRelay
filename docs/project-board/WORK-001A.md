# WORK-001A – Mode-Config Parity & Capability Flags

## Goal
Ensure a single config schema applies unchanged to in-proc, sidecar, and headless edge hosts, with any deviations surfaced via capability flags.

## Scope
- Normalize listener/pipeline defaults per mode; document mode-specific overrides.
- Add capability advertisement for unsupported features per mode (e.g., socket options, buffers).
- Update config validation to fail fast on mode-incompatible settings with actionable errors.

## Deliverables
- Updated config schema and validator.
- Capability flag emission in admin/health endpoints.
- Docs snippet: “Mode selection and compatibility.”

## Acceptance Criteria
- Same config file boots all three hosts; if not, validation fails with clear message and capability flag indicates the gap.
- Unit + integration tests covering mode-specific validation paths.
- AOT publish passes for all hosts touched.

## Status
Open
