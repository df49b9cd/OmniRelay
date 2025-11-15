# REFDISC-029 - Configuration Reload & Watcher Services

## Goal
Provide a reusable configuration watcher module that monitors files/environment changes, validates updates, and applies them safely across dispatcher and control-plane services without restarting processes.

## Scope
- Extract configuration reload logic (file watchers, debounce, options monitors) from dispatcher hosting.
- Offer APIs to register configuration sections with validation-before-apply callbacks.
- Integrate with diagnostics runtime to surface reload status and errors.
- Document how to enable/disable hot reload per service and handle failures gracefully.

## Requirements
1. **Safe reload** - Validate new configuration snapshots before applying; revert/rollback on failure with clear logging.
2. **Debounce** - Avoid thrash by coalescing rapid file changes with configurable delays.
3. **Multi-source support** - Monitor JSON files, environment variables, and optional external configuration sources.
4. **Observability** - Emit metrics/logs for reload events, failures, and applied sections.
5. **Isolation** - Module must not depend on dispatcher-specific types; callbacks receive strongly typed options.

## Deliverables
- Configuration watcher service/library (`OmniRelay.Configuration.Watchers`).
- Dispatcher refactor to use the watcher for relevant options.
- Control-plane services updated to opt into reload behavior via the module.
- Documentation on configuration reload patterns, safety, and diagnostics.

## Acceptance Criteria
- Dispatcher hot-reload behavior (e.g., logging level, transport limits) matches previous implementation.
- Control-plane services can enable reloads with consistent validation + error reporting.
- Reload events surface via diagnostics endpoints/logs with actionable messages.
- Module introduces no dispatcher runtime dependencies.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Test debounce logic and ensure multiple file change events produce a single reload.
- Validate validation callbacks and rollback behavior when validation fails.
- Ensure watchers can monitor multiple sections simultaneously without interference.

### Integration tests
- Modify appsettings/env values for running hosts and confirm reload occurs, applying new settings.
- Introduce invalid configuration to verify rollback + error logging.
- Expose reload status via diagnostics endpoints and assert accuracy.

### Feature tests
- In OmniRelay.FeatureTests, enable config reload for dispatcher/control-plane hosts, change settings (e.g., limiter thresholds), and verify runtime behavior updates.
- Simulate operator workflows (toggle features) using config reload and ensure consistent results.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, roll out config changes across many nodes via file updates and ensure watcher scales without overwhelming hosts.
- Stress rapid successive changes to confirm debounce prevents thrash.

## References
- Existing configuration reload logic in `OmniRelay.Configuration`.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
