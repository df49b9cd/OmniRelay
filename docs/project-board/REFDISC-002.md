# REFDISC-002 - HTTP/3 Client Factory for Control Plane

## Goal
Provide a reusable gRPC client/channel factory that encapsulates HTTP/3 preference, downgrade policy, client mTLS, and transport metrics so gossip, leadership, and discovery components can issue control-plane RPCs without duplicating `GrpcOutbound` internals.

## Scope
- Extract channel construction, `SocketsHttpHandler` tuning, and TLS selection logic from `GrpcOutbound`.
- Create a DI-friendly `IGrpcControlPlaneClientFactory` that supplies configured `GrpcChannel`/`HttpClient` instances keyed by endpoint metadata or named profiles.
- Support endpoint-level HTTP/3 capability hints plus runtime overrides (force HTTP/2) based on health probes.
- Ensure control-plane consumers can request lightweight unary clients (for gossip/leadership) without registering dispatcher middleware.

## Requirements
1. **HTTP/3 preference** - Factory must default to HTTP/3 with automatic fallback to HTTP/2 by setting `HttpVersionPolicy.RequestVersionOrLower`; it must expose switches to disable HTTP/3 per endpoint.
2. **Client mTLS** - Support client certificate loading, custom validation callbacks, and revocation checking identical to `GrpcOutbound.ApplyClientTlsOptions`.
3. **Runtime knobs** - Expose limits for max message sizes, ping delays/timeouts, and retry/backoff policies so control-plane traffic can tune reliability.
4. **Peer awareness** - Provide hooks for peer chooser + circuit breaker injection so service discovery utilities can balance across multiple control-plane replicas.
5. **Observability** - Emit the same transport metrics (e.g., `grpc_outbound_requests_total`, RTT histograms) and tracing spans as dispatcher outbound calls.

## Deliverables
- Factory interface + implementation under `OmniRelay.Transport.Grpc`.
- Refactoring of `GrpcOutbound` to consume the factory (removing duplicate handler setup).
- Updates to gossip/leadership/service-discovery agents to resolve the factory rather than newing `HttpClient`/`GrpcChannel`.
- Documentation outlining recommended factory profiles (control-plane vs. data-plane) and configuration keys.

## Acceptance Criteria
- Dispatcher outbound calls continue working via the refactored `GrpcOutbound`.
- Control-plane agents can dial peers over gRPC HTTP/3 using the factory without referencing dispatcher classes.
- Client mTLS failures surface as actionable errors and never silently downgrade to insecure transport.
- Telemetry dashboards observe unchanged metrics despite the refactor.
- Endpoint hints to disable HTTP/3 for specific peers take effect without restarting the process.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate handler creation under combinations of TLS settings, HTTP version policies, and message size limits.
- Exercise peer chooser + circuit breaker injection using fake endpoints to ensure round-robin, preferred-peer, and degraded modes behave.
- Confirm telemetry hooks fire by injecting fake meters/tracers and asserting recorded tags (endpoint, protocol version).

### Integration tests
- Establish gRPC control-plane calls between two hosts using the factory, verifying HTTP/3 handshake and fallback with QUIC disabled.
- Rotate client certificates mid-run and ensure the factory reloads credentials without recreating the host service.
- Simulate endpoint hint updates (e.g., disable HTTP/3) and confirm new calls negotiate HTTP/2 while existing HTTP/3 streams drain gracefully.

### Feature tests
- Within OmniRelay.FeatureTests, wire the factory into gossip + leadership flows and validate peer lookups, circuit-breaker trips, and telemetry surfaces behave identically to pre-refactor HTTP clients.
- Verify operator workflows that scale peers up/down still converge because the factory respects peer chooser updates emitted by service discovery.

### Hyperscale Feature Tests
- In OmniRelay.HyperscaleFeatureTests, drive thousands of rapid control-plane calls through the factory, monitor connection pools, and ensure HTTP/3 downgrades under load do not exhaust sockets.
- Run chaos scenarios (packet loss, certificate expiry) to validate the factory’s retry + mTLS enforcement keeps the control plane healthy.

## References
- `src/OmniRelay/Transport/Grpc/GrpcOutbound.cs` - Existing client/channel setup.
- `docs/architecture/service-discovery.md` - Control-plane client transport expectations.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
