# WORK-006 – Epic: Control Protocol & Capability Negotiation

Split into iteration-sized stories (A–D).

## Child Stories
- **WORK-006A** – Protobuf schemas & versioning policy
- **WORK-006B** – Watch streams (deltas/snapshots) with resume/backoff
- **WORK-006C** – Capability negotiation handshake
- **WORK-006D** – Error/observability semantics

## Definition of Done (epic)
- Protocol implemented with negotiation, retries, and observability; used by MeshKit ↔ OmniRelay paths over mTLS.
