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

## Testing Strategy
- Unit: Cover new logic/config parsing/helpers introduced by this item.
- Integration: Exercise end-to-end behavior via test fixtures (hosts/agents/registry) relevant to this item.
- Feature: Scenario-level validation of user-visible workflows touched by this item across supported deployment modes/roles.
- Hyperscale: Run when the change affects runtime/throughput/scale; otherwise note non-applicability with rationale in the PR.
