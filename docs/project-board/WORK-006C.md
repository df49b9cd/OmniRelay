# WORK-006C – Capability Negotiation Handshake

## Goal
Add capability exchange between OmniRelay/agents and MeshKit, tailoring payloads to supported features.

## Scope
- Client advertises capabilities (runtimes, limits, build epoch).
- Server tailors payloads or rejects with actionable errors.
- Tests for mixed-capability fleets.

## Acceptance Criteria
- Nodes with missing capabilities receive down-leveled payloads or clear rejection; LKG used safely.
- Metrics/logs emitted for negotiation outcomes.

## Status
Open
