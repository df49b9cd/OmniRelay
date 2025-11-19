# WORK-006 – CLI & Diagnostics Client Helpers

## Goal
Provide a shared HTTP/3 + gRPC client helper library that encapsulates transport builders, mTLS/auth, retries, serialization contexts, and streaming helpers so the OmniRelay CLI, MeshKit modules, and third-party automation all share the same control-plane plumbing.

## Scope
- Extract client setup from `OmniRelay.Cli` into `MeshKit.ControlPlane.Client` (or similar) using OmniRelay transport factories/TLS manager.
- Offer typed helpers for common diagnostics calls (`/meshkit/peers`, `/meshkit/shards`, `/omnirelay/control/*`, leadership/shard streams) with resume token handling.
- Support streaming, pagination, retries, and downgrade telemetry out of the box.
- Document usage patterns for CLI, automation scripts, and service integrations.

## Requirements
1. **Auth parity** – Support mTLS client certs, bearer tokens, and future providers; integrate with secret manager abstractions.
2. **Protocol support** – Default to HTTP/3/gRPC with downgrade fallback; expose negotiated protocol metadata for telemetry.
3. **Error handling** – Normalize HTTP/gRPC failures into typed exceptions with remediation hints.
4. **Serialization** – Use shared registry/diagnostics models + source-generated serializers for trimming/AOT safety.
5. **Extensibility** – Allow new endpoints to register typed clients without duplicating lower-level transport logic.

## Deliverables
- Client helper library, tests, docs, and sample usage.
- CLI refactor to depend on the helpers.
- Guidance for third-party/automation consumers.

## Acceptance Criteria
- CLI continues to function unchanged (output, errors) using the helper library.
- MeshKit services/automation scripts can reuse helpers with minimal configuration.
- Helpers reuse OmniRelay transport/TLS builders; telemetry counters stay consistent.
- Native AOT publish/tests succeed per WORK-002..WORK-005.

## Testing Strategy
- Unit: auth handler injection, error translation, serialization round-trips, streaming/pagination helpers.
- Integration: helper calls against test MeshKit hosts verifying handshake, auth, retries, downgrades.
- Feature: CLI + automation workflows validated via helper library.
- Hyperscale: stress helper concurrency, connection pooling, and auth caching.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`