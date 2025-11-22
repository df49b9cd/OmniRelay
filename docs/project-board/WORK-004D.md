# WORK-004D – Signing & SBOM Pipeline

## Goal
Add signing and SBOM generation/verification for all OmniRelay artifacts (packages, binaries, containers).

## Scope
- Integrate signing into publish pipelines.
- Generate SBOMs; store alongside artifacts.
- Verification step in CI/nightly.

## Acceptance Criteria
- All artifacts are signed; verification step passes in CI.
- SBOM produced and archived for each build.

## Status
Open
