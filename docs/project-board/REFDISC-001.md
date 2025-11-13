# REFDISC-001 - Shared HTTP/3 Control Host

## Goal
Factor the dispatcher’s gRPC inbound HTTP/3 + mTLS setup into a reusable host builder so mesh gossip, leadership, and service-discovery control planes can expose gRPC endpoints without instantiating a full dispatcher runtime.

## Scope
- Extract the QUIC/Kestrel configuration, endpoint binding, and TLS enforcement logic currently embedded in `GrpcInbound`.
- Produce a lightweight `GrpcHttp3HostBuilder` (or similar) that accepts service registrations + TLS/runtime options and returns a configured `WebApplication`.
- Ensure the host builder wires the existing server interceptor registry, compression providers, telemetry toggles, and diagnostics middleware when provided.
- Document how control-plane services compose the builder and register their proto services without taking a dependency on `Dispatcher`.

## Requirements
1. **HTTP/3 downgrade** - Host must negotiate HTTP/3 when supported and automatically downgrade to HTTP/2 with `HttpProtocols.Http1AndHttp2AndHttp3`, mirroring dispatcher defaults.
2. **mTLS enforcement** - Support `ClientCertificateMode.RequireCertificate`, certificate revocation toggles, and thumbprint pinning exactly as `GrpcInbound` does today.
3. **Option parity** - Runtime knobs for keep-alive, QUIC idle timeout, bidirectional stream limits, and compression settings must be exposed so existing dispatcher config values map 1:1.
4. **Interceptor support** - The builder must accept interceptor registry instances so logging/tracing/auth policies attach identically across dispatcher and control-plane services.
5. **Observability** - Host must emit the current transport metrics/events (QUIC diagnostics, OpenTelemetry spans) and surface them through the normal diagnostics registration path.

## Deliverables
- New transport builder API + implementation in `OmniRelay.Transport.Grpc`.
- Refactoring of `GrpcInbound` to consume the shared builder (proving backwards compatibility).
- Wiring updates for gossip/leadership hosts to consume the builder instead of ad-hoc `WebApplication` construction.
- Documentation entry describing configuration keys + migration steps for service-discovery components.

## Acceptance Criteria
- Dispatcher gRPC inbound continues to bind successfully with the shared builder and honors all existing TLS/runtime settings.
- Mesh gossip can host gRPC control endpoints through the builder without referencing `Dispatcher`.
- HTTP/3 negotiation + downgrade behavior verified via integration tests against servers with QUIC enabled/disabled.
- Interceptor pipelines registered through the builder fire for both dispatcher and control-plane endpoints.
- Prometheus/OpenTelemetry transport metrics remain unchanged when the builder is adopted.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Cover option translation (keep-alive, idle timeout, stream limits) to ensure invalid settings throw the same exceptions previously produced by `GrpcInbound`.
- Validate TLS/mTLS configuration helpers (certificate selector, client validation callback) using fake certificates to confirm required/optional modes behave identically.
- Exercise interceptor registration plumbing to guarantee duplicate registrations and null registries are handled gracefully.

### Integration tests
- Spin up hosts with HTTP/3 enabled and assert QUIC connections are accepted; repeat with QUIC disabled to confirm graceful downgrade to HTTP/2.
- Verify end-to-end mTLS by presenting valid/invalid client certificates and ensuring rejection occurs before the service handler executes.
- Ensure diagnostics endpoints (e.g., transport health) remain reachable and expose the same data before and after adopting the builder.

### Feature tests
- Use OmniRelay.FeatureTests to run dispatcher + gossip roles simultaneously, ensuring both rely on the shared builder and can be started/stopped independently without socket contention.
- Validate operator workflows (enable/disable HTTP/3 via config) in the feature harness, confirming the builder applies the new settings without requiring code changes.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, start dozens of control-plane hosts using the builder, inject rolling restarts, and confirm QUIC stream limits and TLS renegotiation remain stable.
- Run stress scenarios alternating HTTP/3 availability (forcing downgrades) to ensure connection churn does not regress latency/throughput expectations.

## References
- `src/OmniRelay/Transport/Grpc/GrpcInbound.cs` - Current HTTP/3 + TLS configuration logic.
- `docs/architecture/service-discovery.md` - Control-plane transport requirements.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
