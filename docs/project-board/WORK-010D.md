# WORK-010D – Admission & Compatibility Checks

## Goal
Validate extension artifacts against ABI/runtime requirements and node capabilities before rollout.

## Scope
- Admission pipeline enforcing signatures, ABI match, dependency checks, and capability requirements.
- Error reporting integrated with registry APIs.

## Acceptance Criteria
- Incompatible or unsigned artifacts are rejected with actionable errors.
- Tests cover capability mismatch, bad signatures, missing dependencies.

## Status
Open
