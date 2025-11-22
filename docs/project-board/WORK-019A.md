# WORK-019A – Signing/Verification Pipeline

## Goal
Implement signing for binaries, containers, configs, and extension artifacts with verification in agents/OmniRelay.

## Scope
- Integrate signing into build/publish; verification hooks on load/apply.
- Key management with rotation hooks.

## Acceptance Criteria
- Unsigned/invalid artifacts/configs rejected in tests; signatures verified in CI.

## Status
Open
