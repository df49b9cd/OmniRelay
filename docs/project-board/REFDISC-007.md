# REFDISC-007 - HTTP Control-Plane Client Factory

## Goal
Create a reusable HTTP client factory that encapsulates dispatcher-grade retry policies, mTLS, and middleware pipelines so REST-based control-plane clients (gossip, leadership, bootstrap) no longer maintain bespoke `HttpClient` instances.

## Scope
- Extract handler/policy setup from `HttpOutbound` and related middleware into an `IHttpControlPlaneClientFactory`.
- Support HTTP/2 (with HTTP/1.1 fallback) plus configurable timeouts, concurrency limits, and decompression.
- Integrate with the shared TLS manager to supply client certificates and validation callbacks.
- Allow attaching HTTP middleware (auth headers, logging, tracing) via a shared registry similar to gRPC interceptors.

## Requirements
1. **Protocol features** - Default to HTTP/2 when endpoints support it, auto-downgrade to HTTP/1.1, and surface configuration to force a specific version.
2. **Retry/backoff** - Provide configurable retry policies (status-code aware) and circuit breaker hooks consistent with dispatcher HTTP behavior.
3. **mTLS client auth** - Load client certificates through the shared manager and enforce revocation/thumbprint policies.
4. **Middleware support** - Allow stacking of reusable middleware (auth, tracing, rate limiting) through DI rather than hardcoding per client.
5. **Telemetry** - Emit the same HTTP transport metrics (`omnirelay_transport_http_*`) and tracing spans for both data-plane and control-plane requests.

## Deliverables
- Client factory implementation + DI registrations under `OmniRelay.Transport.Http`.
- Refactor of `HttpOutbound` to use the factory.
- Updates to control-plane services (e.g., gossip HTTP fallbacks, bootstrap clients) to resolve the factory instead of newing `HttpClient`.
- Documentation describing usage patterns, configuration keys, and migration guidance.

## Acceptance Criteria
- Dispatcher HTTP outbound behavior remains unchanged after adopting the factory.
- Control-plane clients obtain configured `HttpClient` instances from the factory with mTLS enabled.
- Retry/backoff and circuit-breaker policies apply uniformly to both dispatcher and control-plane HTTP calls.
- Metrics and tracing emitted by HTTP requests remain consistent.
- Configuration updates (timeouts, HTTP version policy) take effect without code changes.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Verify handler composition for different protocol/version settings, ensuring invalid combinations throw.
- Validate retry/backoff policy generation for various HTTP status codes and failure modes.
- Exercise middleware pipeline registration, confirming ordering and dependency injection behave as expected.

### Integration tests
- Use the factory to call a test server supporting HTTP/2, then disable HTTP/2 to ensure clients downgrade gracefully.
- Rotate client certificates and confirm requests continue succeeding without recreating the client.
- Inject transient failures to ensure retry/backoff policies fire and that circuit breakers prevent storming.

### Feature tests
- Within OmniRelay.FeatureTests, swap existing dispatcher HTTP outbounds to the factory and run end-to-end workflows, verifying requests succeed and telemetry matches baselines.
- Use the factory for control-plane HTTP calls (e.g., diagnostics fetches) and confirm parity with dispatcher behavior.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, run large-scale HTTP control-plane traffic through the factory, ensuring connection pools are reused and retries don’t overload endpoints.
- Simulate throttling/429 responses to validate circuit breakers and rate-limiters recover gracefully.

## References
- `src/OmniRelay/Transport/Http/HttpOutbound.cs` - Current handler/policy implementation.
- `docs/architecture/service-discovery.md` - HTTP client requirements for control-plane services.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
