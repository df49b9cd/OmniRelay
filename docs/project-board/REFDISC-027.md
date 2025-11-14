# REFDISC-027 - Policy-Based Authorization Module

## Goal
Provide a shared authorization module that defines policies (role-based, mesh metadata-based) and integrates with HTTP/gRPC middleware so all control-plane services enforce consistent access control.

## Scope
- Extract policy definitions, claim mappings, and middleware from dispatcher endpoints.
- Support mTLS-derived identities, bearer tokens, and future auth mechanisms.
- Provide DI extensions to register policies and apply them to endpoints/services.
- Document configuration and operational usage for operators.

## Requirements
1. **Policy flexibility** - Allow policies based on roles, clusters, regions, and custom labels.
2. **Auth mechanisms** - Support mTLS client cert mapping, JWT validation, and pluggable providers.
3. **Diagnostics** - Emit logs/metrics for authorization successes/failures and expose endpoints to inspect current policy assignments.
4. **Integration** - Provide middleware/interceptors for ASP.NET Core + gRPC to enforce policies declaratively.
5. **Configuration** - Bind policies via configuration kit with validation and dynamic reload when supported.

## Deliverables
- Authorization module (`OmniRelay.Security.Authorization`) with policy builders and middleware.
- Dispatcher refactor to use the module for existing auth checks.
- Control-plane services updated to declare policies via the module.
- Documentation covering policy syntax, configuration, and diagnostics.

## Acceptance Criteria
- Existing authorization behavior (roles, clusters) remains unchanged after migration.
- Control-plane services enforce the same policies without dispatcher dependencies.
- Diagnostics endpoints/logs reflect authorization decisions uniformly.
- Policy updates via configuration take effect without restarts when supported.
- Module is dispatcher-agnostic.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate policy evaluation logic for various claim combinations and rule sets.
- Test JWT + mTLS identity mapping and failure paths.
- Ensure configuration parsing detects invalid policies.

### Integration tests
- Apply policies to HTTP/gRPC endpoints, issue authorized/unauthorized requests, and verify responses/logs.
- Reload policy configuration and confirm changes take effect.
- Inspect diagnostics endpoints to ensure policy metadata is exposed.

### Feature tests
- In OmniRelay.FeatureTests, enforce policies on dispatcher/control-plane endpoints and run operator scenarios (role changes, access revocations).
- Validate CLI/automation clients handle authorization errors consistently via shared helpers.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, run many authorized/unauthorized requests to ensure policy evaluation scales and telemetry remains accurate.
- Stress dynamic policy updates to confirm consistent propagation.

## References
- Current authorization logic in dispatcher HTTP/gRPC endpoints and diagnostics docs.
- REFDISC-034..037 - AOT readiness baseline and CI gating.

## Implementation status
- Authorization policies are now bound via `security.authorization` and compiled into `MeshAuthorizationEvaluator` instances. HTTP inbounds run a shared middleware that enforces role/cluster/header requirements plus optional mutual TLS flags, while gRPC inbounds use a `MeshAuthorizationGrpcInterceptor` so both stacks reject unauthorized requests with consistent `403/PermissionDenied` payloads.
- Policies support transport-specific path prefixes, principal allow-lists, and label checks without referencing dispatcher-specific code. Configuration lives entirely in DI, enabling control-plane services to reuse the evaluator and interceptors.
