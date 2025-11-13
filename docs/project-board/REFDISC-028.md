# REFDISC-028 - Schema & Documentation Generation Utilities

## Goal
Refactor API documentation (OpenAPI, gRPC reflection, control-plane docs) into reusable utilities so all hosts can expose consistent schemas and operator-facing documentation automatically.

## Scope
- Extract OpenAPI generation, gRPC reflection configuration, and doc publishing logic from dispatcher setup.
- Provide helpers to register documentation endpoints with shared authentication and metadata.
- Integrate with authorization module to gate docs when required.
- Document how services configure schema generation and customize metadata.

## Requirements
1. **OpenAPI parity** - Generated OpenAPI documents must match dispatcher’s existing output (tags, descriptions).
2. **gRPC reflection** - Enable/disable reflection consistently across hosts with TLS/auth awareness.
3. **Customization** - Allow services to add metadata (contact info, versioning) without editing core code.
4. **Security** - Support optional authentication for doc endpoints and ensure sensitive operations aren’t exposed unintentionally.
5. **Automation** - Provide tooling hooks for CLI/CI to fetch schemas for validation.

## Deliverables
- Schema/doc generation utilities (`OmniRelay.Diagnostics.Documentation`).
- Dispatcher refactor to use the utilities for existing docs endpoints.
- Control-plane services updated to expose docs via the utilities.
- Documentation describing configuration, security, and automation workflows.

## Acceptance Criteria
- OpenAPI/gRPC schema output remains unchanged after migration (verified via diff).
- Control-plane services can expose docs with minimal configuration and optional auth.
- CLI/tooling can fetch schemas using shared helpers.
- Doc endpoints respect authorization policies and telemetry.
- Utilities are free of dispatcher runtime dependencies.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate metadata customization (titles, descriptions) is applied correctly.
- Ensure configuration flags enable/disable doc generation as expected.
- Test security options (auth required) to confirm enforcement.

### Integration tests
- Generate OpenAPI docs for sample hosts, compare against known baselines.
- Enable gRPC reflection and verify clients can discover services over HTTP/2/3.
- Fetch docs via CLI automation tests to ensure compatibility.

### Feature tests
- In OmniRelay.FeatureTests, expose docs for dispatcher/control-plane services and verify operator access patterns (authenticated vs. public).
- Validate doc endpoints respect authorization policies from REFDISC-027.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, ensure doc endpoints remain responsive even when many services expose them simultaneously.
- Test automation workflows fetching schemas from large fleets to ensure performance and stability.

## References
- Current OpenAPI/gRPC reflection setup in dispatcher hosting.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
