# WORK-023B – Packaging & Multi-Targeting

## Goal
Make the shared libraries NuGet-packable/internal-feed ready, multi-targeting `net10.0` and AOT-safe `net10.0` with `#if NATIVE_AOT` guards, and publish them for consumption by MeshKit/OmniRelay builds.

## Scope
- NuGet metadata, internal feed publishing, and versioning strategy.
- Multi-targeting; guard AOT-sensitive code paths; avoid reflection/dynamic load.
- CI jobs to build/package and push these libraries; OmniRelay and MeshKit consume via project/package refs.

## Acceptance Criteria
- Packages publish successfully for both targets; AOT publish passes.
- Packages signed and include symbols/SBOM per policy.

## Status
Open

## Testing Strategy
- Unit: Cover new logic/config parsing/helpers introduced by this item.
- Integration: Exercise end-to-end behavior via test fixtures (hosts/agents/registry) relevant to this item.
- Feature: Scenario-level validation of user-visible workflows touched by this item across supported deployment modes/roles.
- Hyperscale: Run when the change affects runtime/throughput/scale; otherwise note non-applicability with rationale in the PR.
