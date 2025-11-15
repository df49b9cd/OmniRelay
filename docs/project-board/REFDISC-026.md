# REFDISC-026 - Credential & Secret Management Abstractions

## Goal
Refactor secret loading (cert passwords, API keys, tokens) into a reusable abstraction so dispatcher and control-plane services retrieve secrets securely from files, environment variables, or external vaults with consistent auditing.

## Scope
- Extract secret provider logic from bootstrap/runtime components.
- Provide interfaces and implementations for file-based, env-based, and external vault-backed secret retrieval.
- Integrate with the shared TLS manager and configuration kit.
- Document best practices for secret handling and auditing.

## Requirements
1. **Secure retrieval** - Ensure secrets are read securely (zeroed after use) and support async refresh.
2. **Provider precedence** - Allow configuring provider order (vault > env > file) with clear fallback behavior.
3. **Auditing** - Emit logs/metrics for secret access (without leaking values) and failures.
4. **Rotation support** - Provide hooks to detect secret changes and notify dependent components.
5. **Extensibility** - Allow registering custom providers (e.g., cloud KMS) via DI.

## Deliverables
- Secret management library (`OmniRelay.Security.Secrets`).
- Dispatcher refactor to consume the library for all secret access.
- Control-plane services updated to retrieve secrets via the abstraction.
- Documentation covering provider configuration, rotation, and auditing.

## Acceptance Criteria
- Existing secret loading behavior (env/file) continues to work with minimal configuration changes.
- Control-plane services can opt into vault providers without dispatcher dependencies.
- Secret access logs/metrics provide visibility without exposing sensitive data.
- Rotation notifications propagate to TLS manager and other consumers.
- Library contains no dispatcher-specific references.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Verify provider precedence and fallback behavior.
- Test secret zeroization and disposal semantics.
- Validate rotation hooks trigger when underlying secret sources change.

### Integration tests
- Configure sample hosts with different providers (file/env/vault mock) and ensure secrets load correctly.
- Rotate secrets and confirm consumers receive updated values.
- Simulate provider failures to ensure errors are surfaced and logged.

### Feature tests
- In OmniRelay.FeatureTests, inject secrets via different providers and run workflows (certificate loading) to ensure behavior matches expectations.
- Validate operator tooling for forcing secret refreshes or inspecting provider status.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, run many hosts retrieving secrets concurrently to ensure providers scale and logging remains manageable.
- Stress rotation scenarios (bulk updates) to confirm notification fan-out works.

## References
- Existing secret handling in bootstrap/gossip TLS configuration.
- REFDISC-034..037 - AOT readiness baseline and CI gating.

## Implementation status
- Secrets now flow through the shared `ISecretProvider` abstraction in `src/OmniRelay/Security/Secrets`. Environment, inline, and file-based providers can be composed with precedence, audited, and watched for rotation. `TransportTlsManager`, gossip TLS, and gRPC/HTTP host builders accept secret-backed certificate data/passwords so operators can move sensitive material out of appsettings without losing reload semantics. Tests cover zeroization, provider precedence, and change token invalidation.
