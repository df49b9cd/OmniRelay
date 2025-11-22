# WORK-004C – Headless Edge Binary/Image

## Goal
Deliver headless edge binary and container image with capability manifest, ready for ingress/egress scenarios.

## Scope
- Build edge host binary; ship container variant.
- Include capability manifest and health endpoints.
- Document sizing and deployment notes.

## Acceptance Criteria
- Edge host runs with provided config; health endpoints live.
- Manifest consumed by MeshKit; published per RID.

## Status
Open

## Testing Strategy
- Unit: Cover new logic/config parsing/helpers introduced by this item.
- Integration: Exercise end-to-end behavior via test fixtures (hosts/agents/registry) relevant to this item.
- Feature: Scenario-level validation of user-visible workflows touched by this item across supported deployment modes/roles.
- Hyperscale: Run when the change affects runtime/throughput/scale; otherwise note non-applicability with rationale in the PR.
