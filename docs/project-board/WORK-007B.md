# WORK-007B – Rotation/Renewal & Trust Bundle Delivery

## Goal
Automate certificate renewal and distribute trust bundles to agents/OmniRelay.

## Scope
- Renewal scheduler with backoff; grace/LKG handling.
- Trust bundle publication and retrieval.
- Client logic in agent/OmniRelay to renew and reload certs without traffic loss.

## Acceptance Criteria
- Renewal occurs before expiry; traffic continues uninterrupted in tests.
- Trust bundle updates propagate; stale bundles detected and refreshed.

## Status
Open
