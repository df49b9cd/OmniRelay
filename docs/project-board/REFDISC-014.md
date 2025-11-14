# REFDISC-014 - Bootstrap & Join Toolkit

## Goal
Extract the dispatcher’s bootstrap/join logic (certificate provisioning, seed distribution, join tokens) into a reusable toolkit so control-plane services can host or consume onboarding flows without duplicating code.

## Scope
- Isolate bootstrap client/server components, including REST/gRPC join endpoints, token validation, and secret handling.
- Provide libraries to generate/join bootstrap bundles (certificates, seed peer lists) referenced by both dispatcher and auxiliary services.
- Ensure tooling can run headless (CLI, automation) while sharing the same validation rules and telemetry as the dispatcher.
- Document onboarding flows for operators integrating new control-plane services.

## Requirements
1. **Secure token handling** - Join tokens must be validated with replay protection, expiration, and binding to intended clusters.
2. **Certificate issuance** - Toolkit must integrate with existing PKI flows to request, sign, and deliver mTLS certificates (file or inline).
3. **Seed distribution** - Provide APIs to fetch initial peer lists/metadata, supporting HTTP/3 and fallback to HTTP/2 with mTLS.
4. **Audit trail** - Emit structured logs/metrics for bootstrap attempts (success/failure, reasons) to meet compliance requirements.
5. **CLI integration** - Offer shared helpers so the OmniRelay CLI and other tools consume the same bootstrap APIs and error messages.

## Deliverables
- Bootstrap toolkit (client/server libraries, token utilities) under `OmniRelay.ControlPlane.Bootstrap`.
- Refactor of existing bootstrap/join endpoints to use the toolkit.
- CLI updates to rely on the shared helpers instead of bespoke logic.
- Documentation covering onboarding flows, configuration, and security hardening.

## Acceptance Criteria
- Dispatcher bootstrap/join behavior remains unchanged after refactor (same API contracts, logs).
- Control-plane services can host join endpoints or consume bootstrap bundles via the toolkit without referencing dispatcher internals.
- CLI/automation scripts can use shared helpers and receive consistent error messages.
- Bootstrap metrics/logs feed into existing observability pipelines.
- Security review approves token/certificate handling in the new toolkit.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate token generation/validation (expiration, audience binding, signature).
- Cover certificate issuance flows (file output, inline data) including failure cases.
- Test seed list serialization/deserialization for different cluster configurations.

### Integration tests
- Spin up a test bootstrap server using the toolkit, perform join flows with valid/invalid tokens, and assert responses + telemetry.
- Ensure certificate issuance integrates with PKI mocks and handles rotation gracefully.
- Run CLI-driven onboarding against the toolkit to verify compatibility.

### Feature tests
- In OmniRelay.FeatureTests, provision new nodes via the toolkit, ensuring dispatcher and control-plane services bootstrap identically.
- Exercise operator workflows (token revocation, reissue) to confirm toolkit surfaces the same controls as today.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, execute mass onboarding scenarios (dozens of nodes) to ensure token validation + certificate issuance scale without throttling.
- Inject malicious/replay attempts to confirm toolkit rejects them and emits alerts.

## References
- Current bootstrap/join implementation (check `docs/architecture/security-bootstrap.md` and CLI sources).
- REFDISC-034..037 - AOT readiness baseline and CI gating.

## Implementation status
- `OmniRelay.ControlPlane.Bootstrap` now includes token services, replay protection, HTTP host helpers, and a client library. Dispatcher wiring lights up a `/omnirelay/bootstrap/join` host when `security.bootstrap.*` is configured, reusing the shared `TransportTlsManager` to export certificates plus configured seed peers.
- The CLI exposes `omnirelay mesh bootstrap issue-token` and `join` commands so operators can mint HMAC-backed join tokens and request bundles against any bootstrap service. Both commands use the shared token service + HTTP client.
- Configuration gains a `security.bootstrap` block (URLs, TLS, seed peers, signing keys) so control-plane services and automation scripts can host or consume onboarding without touching dispatcher internals.
