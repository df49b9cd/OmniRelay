# REFDISC-006 - Shared HTTP Control Host

## Goal
Refactor the dispatcher’s HTTP inbound host (Kestrel + middleware wiring) into a reusable builder so REST-based control-plane services can expose endpoints with the same TLS, routing, and diagnostics behavior without depending on `Dispatcher`.

## Scope
- Extract the ASP.NET Core/Kestrel configuration from `HttpInbound`/`HttpInboundMiddleware` into a neutral `HttpControlPlaneHostBuilder`.
- Support registering minimal APIs/controllers plus shared middleware (auth, rate limiting, tracing) via DI.
- Ensure TLS/mTLS enforcement mirrors the gRPC host builder, including client-certificate requirements and revocation toggles.
- Provide hooks to attach diagnostics endpoints (`/omnirelay/control/*`) even when the dispatcher is absent.

## Requirements
1. **Protocol parity** - Support HTTP/1.1 and HTTP/2 with ALPN negotiation, honoring dispatcher defaults for keep-alive, request limits, and backpressure.
2. **TLS/mTLS** - Allow optional/required client certificates, certificate validation overrides, and shared TLS manager integration (REFDISC-003).
3. **Middleware registration** - Accept pipelines composed of dispatcher HTTP middleware (auth, logging, tracing) via a shared registry rather than hardcoding.
4. **Diagnostics surfacing** - Provide built-in routes for control-plane diagnostics (peers, lease health, logging toggles) with the same authorization patterns.
5. **Configuration** - Bind all existing `transport:http` runtime options from appsettings/environment so hosts are configurable without recompilation.

## Deliverables
- Host builder implementation + DI extensions under `OmniRelay.Transport.Http`.
- Refactor of `HttpInbound` to consume the shared builder.
- Wiring updates for control-plane services (gossip diagnostics, bootstrap REST APIs) to use the builder instead of ad-hoc `WebApplication` creation.
- Documentation detailing configuration keys, TLS guidance, and migration steps.

## Acceptance Criteria
- Dispatcher HTTP inbound behaves identically after adopting the builder.
- Control-plane services can expose REST endpoints via the builder without referencing `Dispatcher`.
- TLS/mTLS enforcement matches dispatcher behavior, including customizable certificate validation.
- Diagnostics endpoints remain available and emit consistent data regardless of host.
- Configuration toggles (keep-alive, limits) update behavior without code changes.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate option parsing for limits (max request body size, keep-alive, timeouts) and ensure invalid values throw informative exceptions.
- Exercise TLS configuration helpers for required/optional client certificates and revocation toggles.
- Test middleware registry wiring to ensure pipelines are composed in the specified order and guard against duplicates.

### Integration tests
- Boot a control-plane host with the builder, hit routes over HTTPS with and without client certificates, and verify enforcement.
- Toggle runtime settings (keep-alive, request limits) via configuration reload and confirm Kestrel updates accordingly.
- Ensure diagnostics endpoints (e.g., `/omnirelay/control/logging`) function even without the dispatcher.

### Feature tests
- In OmniRelay.FeatureTests, host dispatcher REST APIs and control-plane diagnostics simultaneously using the builder, verifying independent lifecycle management.
- Run operator workflows (enable/disable rate limiting, auth modes) and ensure both hosts honor the shared middleware configuration.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, deploy many hosts using the builder, subject them to high connection churn, and confirm limits/backpressure behave uniformly.
- Perform rolling mTLS configuration changes to ensure client certificate enforcement remains aligned across all hosts.

## References
- `src/OmniRelay/Transport/Http/HttpInbound.cs` and `Middleware/` - Source logic to extract.
- `docs/architecture/service-discovery.md` - HTTP control-plane requirements.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
