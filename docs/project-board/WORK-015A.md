# WORK-015A – Policy Compute Core & Deterministic Hashing

## Goal
Build the core policy engine that produces deterministic bundles (routes, clusters, authz) with hash/epoch metadata.

## Scope
- Deterministic computation; include hash/version in outputs.
- Basic validation for references.

## Acceptance Criteria
- Same inputs → same hash; unit tests cover determinism.
- Bundles include hash/epoch metadata.

## Status
Open
