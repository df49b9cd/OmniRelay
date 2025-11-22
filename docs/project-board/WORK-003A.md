# WORK-003A – DSL Host MVP

## Goal
Ship the AOT-friendly DSL interpreter with signed package validation, opcode allowlist, and quotas.

## Scope
- Manifest + signature checks for DSL packages.
- Opcode allowlist and resource quotas (time/memory) with policy defaults.
- Minimal telemetry for load/failure events.

## Acceptance Criteria
- Unsigned/invalid DSL packages rejected.
- Quota breach triggers configured policy and emits telemetry.
- Unit/integration tests run in all modes; AOT publish passes.

## Status
Open
