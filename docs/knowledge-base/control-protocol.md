# Control Protocol (Work-006)

## Schemas & versioning (006A)
- Protobuf: `src/OmniRelay.Protos/Protos/control_plane.proto`.
- Key types:
  - `CapabilitySet { items[], build_epoch }`
  - `WatchResumeToken { version, epoch, opaque }`
  - `ControlWatchRequest { node_id, capabilities, resume_token }`
  - `ControlWatchResponse { version, epoch, payload, full_snapshot, resume_token, backoff, error, required_capabilities[] }`
  - `ControlSnapshotRequest/Response` mirror watch (no resume).
- Versioning policy:
  - **Semantic wire versions** carried in `version` and `epoch`. Increment epoch on breaking schema changes; version for payload changes within an epoch.
  - **Backward-compatible additions**: only add optional/nullable fields; do not repurpose numeric tags.
  - **Deprecation window**: keep fields alive for ≥2 minor versions; document removal in release notes.
  - **Capability gating**: features behind capability strings (e.g., `core/v1`, `dsl/v1`). Servers never emit payloads requiring capabilities the client did not advertise.

## Watch streams with resume/backoff (006B)
- Server (ControlPlaneWatchService):
  - Validates capabilities; on mismatch returns `ControlWatchResponse` with `error.code=unsupported_capability` and `backoff.millis=5000`.
  - Generates resume token `{ version, epoch, opaque=node_id }` for every response.
  - Returns full snapshot when the incoming resume token version ≠ current; otherwise delta/no-op snapshot.
  - Default backoff hint 1000 ms.
- Client (WatchHarness):
  - Applies LKG cache on startup and reuses persisted `resume_token`.
  - On errors, logs and respects server-provided backoff, doubling up to 30 s.
  - Saves version/epoch/payload/resume_token after each successful apply.

## Capability negotiation (006C)
- Client advertises `CapabilitySet` (`items` + `build_epoch`).
- Server checks against its supported set (`core/v1`, `dsl/v1`); if unsupported, sends an error response with remediation text.
- Responses include `required_capabilities` so clients can detect when they are missing a feature and fall back to LKG.

## Errors & observability (006D)
- Error model: `ControlError { code, message, remediation }` embedded in watch responses; typical codes: `unsupported_capability`, `invalid_resume_token` (reserved for future), `apply_failed` (reserved).
- Logging (AgentLog):
  - `ControlWatchError` (code/message), `ControlWatchResume` (resume token), `ControlBackoffApplied` (ms), `ControlUpdateRejected/Applied`, validation timing, LKG applied.
- Metrics/tracing: hooks live in WatchHarness/TelemetryForwarder; integrate with OTLP exporters later.
- Admin visibility: control-plane service exposes required capabilities and backoff in the first response; agents log remediation hints.

## Operational defaults
- Backoff: start 1 s, double to max 30 s; server hint overrides.
- Payload: currently empty placeholder; wiring supports binary bundles (routes/policies/extensions) once produced.
- Security: run over mTLS; opaque resume token is echoed; sanitize before logging if it contains user data.
