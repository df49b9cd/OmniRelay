# REFDISC-012 - Configuration Binding & Validation Kit

## Goal
Consolidate dispatcher configuration binding/validation helpers into a shared kit so all control-plane services parse settings (options, environment overrides, validation) consistently.

## Scope
- Extract configuration helpers (options binding, validation, environment prefix handling) from `OmniRelay.Configuration`.
- Provide extension methods for binding strongly typed options with validation attributes + custom validators.
- Include utilities for normalized path handling, URI validation, and service-name enforcement used across dispatcher/gossip/leadership settings.
- Document recommended configuration structure and override precedence for hosts.

## Requirements
1. **Consistent binding** - Ensure options binding honors environment variables, JSON config, and command-line overrides identically for all services.
2. **Validation** - Support declarative (DataAnnotations) and imperative validators with clear exception messages.
3. **Defaults** - Provide default value providers (e.g., machine name fallbacks) shared across components.
4. **Error reporting** - Surface configuration errors early with actionable messages referencing option paths.
5. **Extensibility** - Allow new services to plug in additional validation without modifying dispatcher-specific code.

## Deliverables
- Configuration kit (extensions, validators, helpers) under `OmniRelay.Configuration.Abstractions` or similar.
- Dispatcher configuration refactored to use the kit.
- Control-plane services updated to bind their options through the shared helpers.
- Documentation on configuration layering, validation examples, and migration steps.

## Acceptance Criteria
- Binding/validation behavior for dispatcher options remains unchanged (verified via existing tests).
- Control-plane services can bind options (e.g., `mesh:gossip`, `mesh:leadership`) with the same validation semantics.
- Misconfigurations produce consistent error messages referencing option keys.
- Default value behavior (service name, node ID) is identical across hosts.
- No dispatcher-specific dependencies remain in the shared kit.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Cover binding scenarios (JSON, environment override, missing values) for representative options.
- Validate custom validators for URIs, file paths, TLS settings, and ensure exceptions include option paths.
- Test default value providers to confirm environment/machine name fallbacks work and can be overridden.

### Integration tests
- Configure sample hosts with valid/invalid settings, ensure startup fails with consistent messages when invalid.
- Verify environment variable overrides take precedence over appsettings without requiring code changes.
- Ensure reloading configuration propagates to options monitors when enabled.

### Feature tests
- In OmniRelay.FeatureTests, exercise configuration toggles (enable/disable gossip, change ports) to ensure behavior matches expectations via the shared kit.
- Validate CLI/tooling workflows that rely on consistent error messages (e.g., invalid TLS path) continue to function.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, deploy many nodes with mixed configuration sources (env, json) and ensure binding/validation remains fast and reliable.
- Simulate config reload storms to confirm options monitors handle rapid updates without leaking memory.

## References
- `src/OmniRelay.Configuration/ServiceCollectionExtensions.cs` and related options binding helpers.
- `docs/architecture/service-discovery.md` - Configuration schema guidance.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
