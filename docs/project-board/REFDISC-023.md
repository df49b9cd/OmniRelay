# REFDISC-023 - Transport Security Policy Enforcement Kit

## Goal
Refactor transport governance (allowlists, protocol enforcement, endpoint policy checks) into a shared kit so dispatcher and control-plane services enforce consistent security policies on inbound/outbound traffic.

## Scope
- Extract policy evaluators, configuration, and enforcement hooks from DISC-013-related work.
- Provide APIs to define allowed protocols, cipher suites, endpoint restrictions, and certificate pinning.
- Integrate with shared TLS managers, transport builders, and middleware.
- Document policy configuration and auditing workflows.

## Requirements
1. **Policy coverage** - Support checks for protocol versions (HTTP/3/2), cipher suites, certificate authorities, and endpoint domains/IP ranges.
2. **Enforcement hooks** - Provide middleware/interceptors that fail requests violating policy with clear errors/logs.
3. **Auditability** - Emit structured logs/metrics for policy violations and allow exporting current policy state.
4. **Configuration** - Bind policies via configuration kit with schema validation.
5. **Extensibility** - Allow new policy types/modules to register without modifying core enforcement code.

## Deliverables
- Security policy kit (`OmniRelay.Transport.Security`) with evaluators + middleware.
- Dispatcher integration replacing ad-hoc checks.
- Control-plane services updated to opt into policy enforcement.
- Documentation covering configuration, auditing, and incident response.

## Acceptance Criteria
- Existing transport governance behavior (deny insecure endpoints/protocols) remains intact after migration.
- Policy violations produce consistent logs/metrics and block traffic as expected.
- Control-plane services can apply the same policies without referencing dispatcher code.
- Configuration errors surface actionable messages referencing specific policy settings.
- Kit remains independent of dispatcher runtime.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate each policy evaluator (protocol version, endpoint allowlist, certificate pinning) with compliant and violating inputs.
- Test configuration parsing/validation for policy documents.
- Ensure middleware/interceptors apply policies and emit expected logs.

### Integration tests
- Apply policies to HTTP/gRPC hosts, attempt compliant/violating connections, and verify outcomes.
- Update policies at runtime (if supported) and confirm new rules apply to subsequent requests.
- Export policy state via diagnostics endpoints and confirm accuracy.

### Feature tests
- In OmniRelay.FeatureTests, enforce policies across dispatcher/control-plane services and run traffic scenarios to ensure consistent enforcement.
- Simulate violation incidents (e.g., rogue endpoint) and validate operator alerts/logs.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, enforce policies across large node fleets, ensuring performance impact stays minimal and violations are tracked accurately.
- Stress policy updates to ensure propagation doesn’t induce downtime.

## References
- DISC-013 requirements and existing transport policy checks.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
