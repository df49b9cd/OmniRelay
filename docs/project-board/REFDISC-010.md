# REFDISC-010 - Unified Telemetry Registration

## Goal
Provide a reusable telemetry module that registers OmniRelay meters, tracers, and logging bridges once per process so dispatcher and control-plane services emit consistent signals without duplicating setup code.

## Scope
- Extract OpenTelemetry registration logic (meters, exporters, runtime diagnostics) from `OmniRelay.Configuration`.
- Offer DI extensions to add the OmniRelay meters (`Peers`, `Gossip`, `Leadership`, `Transport`) and activity sources to any host.
- Centralize configuration for OTLP/Prometheus exporters, sampling toggles, and tracing enablement.
- Document how to consume the telemetry module in both dispatcher and control-plane contexts.

## Requirements
1. **Single registration** - Telemetry components must guard against duplicate registration even if multiple hosts call the extension.
2. **Config parity** - All existing diagnostics configuration options (OpenTelemetry enablement, exporter settings, sampling toggles) must be honored.
3. **Runtime toggles** - Integration with the diagnostics runtime to allow live adjustments of sampling/logging level.
4. **Exporter support** - Continue supporting Prometheus, OTLP metrics/traces, and future exporters with consistent configuration.
5. **Backward compatibility** - Introducing the module must not change metric names, units, or labels.

## Deliverables
- Telemetry module (extensions + configuration) under `OmniRelay.Diagnostics` or similar namespace.
- Dispatcher configuration updated to rely on the module.
- Control-plane hosts updated to call the module when telemetry is enabled.
- Documentation outlining configuration examples and how to add new meters/activity sources.

## Acceptance Criteria
- Metrics and traces emitted by dispatcher remain identical (names + labels) after migration.
- Control-plane services can emit OmniRelay metrics/traces by referencing the module.
- Prometheus and OTLP exporters continue to function with existing appsettings.
- Runtime sampling/logging toggles adjust telemetry behavior across all hosts simultaneously.
- Tests/dashboards built on existing metrics remain valid without modification.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate configuration parsing for exporter options, ensuring invalid endpoints/protocols raise descriptive errors.
- Confirm meters and activity sources register exactly once even when extensions are invoked multiple times.
- Test runtime toggle integration to ensure sampling/logging level adjustments propagate.

### Integration tests
- Stand up hosts with the module enabled, scrape Prometheus endpoint, and verify metric families exist with expected labels.
- Configure OTLP exporters and ensure metrics/traces arrive at a test collector.
- Disable telemetry via configuration and confirm no meters/activity sources register.

### Feature tests
- In OmniRelay.FeatureTests, enable telemetry for dispatcher + control-plane hosts and ensure dashboards/CLI output reflect combined metrics.
- Toggle tracing at runtime via control endpoints and confirm spans are emitted/stopped across all hosts.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, run large node counts with telemetry enabled to ensure registration remains stable and exporters keep up.
- Stress runtime toggles (frequent sampling changes) to ensure the module doesn’t leak resources or double-register meters.

## References
- `src/OmniRelay.Configuration/ServiceCollectionExtensions.cs` - Current telemetry/OTel registration logic.
- `docs/architecture/service-discovery.md` - Diagnostics + observability expectations.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
