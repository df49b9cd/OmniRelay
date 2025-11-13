# REFDISC-024 - Logging Configuration & Enricher Kit

## Goal
Refactor the dispatcher’s logging configuration (providers, filters, enrichers) into a reusable kit so control-plane services can adopt the same structured logging behavior and dynamic filter toggles without duplicating setup code.

## Scope
- Extract logging provider registration, filter rules, and enrichment logic (mesh metadata, correlation IDs) from dispatcher configuration.
- Provide DI extensions to configure Microsoft.Extensions.Logging/Serilog pipelines with shared enrichers.
- Integrate with diagnostics runtime toggles for dynamic log level changes.
- Document usage patterns and configuration options for all hosts.

## Requirements
1. **Provider parity** - Support the same logging providers and filter rules used by the dispatcher (console, OTLP if configured).
2. **Enrichers** - Include enrichers for mesh metadata (nodeId, cluster, role), request ids, and control-plane context.
3. **Dynamic toggles** - Honor diagnostics runtime log-level toggles across all hosts.
4. **Configuration** - Bind logging configuration (category overrides, minimum levels) via the shared configuration kit.
5. **Isolation** - Kit must function without dispatcher-specific dependencies.

## Deliverables
- Logging kit (`OmniRelay.Diagnostics.Logging`) with extensions and enrichers.
- Dispatcher refactor to consume the kit.
- Control-plane services updated to configure logging via the kit.
- Documentation covering configuration, enrichers, and dynamic control.

## Acceptance Criteria
- Dispatcher logging output remains unchanged after migration (verified via sample logs).
- Control-plane services emit logs with the same enrichers and honor dynamic level toggles.
- Configured overrides (per-category levels) work identically across hosts.
- Kit introduces no dispatcher runtime dependencies.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Verify enrichers add expected properties to log state.
- Test configuration parsing for minimum levels and overrides.
- Ensure diagnostics runtime toggles change logging levels on the fly.

### Integration tests
- Spin up hosts using the kit, emit logs, and assert structured output contains mesh metadata.
- Toggle log levels via `/omnirelay/control/logging` and confirm runtime behavior.
- Validate multiple hosts in one process can register logging pipelines without conflicts.

### Feature tests
- In OmniRelay.FeatureTests, compare dispatcher/control-plane log output before and after migration to ensure parity.
- Exercise operator workflows that adjust log levels globally/per-category.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, ensure high-volume logging remains performant and enrichment does not add significant overhead.
- Toggle log levels under load to confirm responsiveness.

## References
- Current logging configuration in `OmniRelay.Configuration`.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
