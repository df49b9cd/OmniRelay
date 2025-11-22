# WORK-023C – MeshKit Integration & Regression Tests

## Goal
Adopt the shared transport/codec/proto packages inside MeshKit and add regression tests to prevent divergence.

## Scope
- Replace MeshKit transport/codec usages with shared libraries.
- Contract tests ensuring MeshKit uses the same behaviors as OmniRelay (in-proc fixtures/in-memory server).
- Guard against future drift via tests in both repos/suites.

## Acceptance Criteria
- MeshKit builds against shared packages; no duplicated transport/codec code remains.
- Regression tests pass in CI (MeshKit + OmniRelay solution).

## Status
Open
