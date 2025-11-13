# REFDISC-025 - Alerting & Notification Framework

## Goal
Provide a shared alerting framework for emitting notifications (webhooks, Slack/email, PagerDuty) from dispatcher and control-plane services when critical events occur, ensuring consistent throttling, formatting, and authentication.

## Scope
- Extract existing alert hooks (peer failures, transport incidents) into reusable services.
- Support multiple notification channels with pluggable senders and templated payloads.
- Integrate with diagnostics runtime to expose alert status and throttling.
- Document configuration and operational usage.

## Requirements
1. **Channel support** - Provide built-in senders for webhooks and common chat/on-call platforms, with extensibility for others.
2. **Templating** - Allow structured message templates with mesh metadata and event context.
3. **Throttling** - Enforce rate limits to prevent alert storms, with configuration per channel/event type.
4. **Authentication** - Support signing, API tokens, and mTLS where applicable for outbound alerts.
5. **Observability** - Emit metrics/logs for alert attempts, successes, failures, and throttling.

## Deliverables
- Alerting framework library (`OmniRelay.Diagnostics.Alerting`).
- Dispatcher refactor to use the framework for existing alerts.
- Control-plane services updated to emit alerts via the framework.
- Documentation covering configuration, channel setup, and monitoring.

## Acceptance Criteria
- Existing alert workflows continue functioning with identical payloads post-migration.
- Control-plane services can configure alerts without dispatcher dependencies.
- Alert throttling and authentication behave consistently across hosts.
- Telemetry dashboards display alert metrics from all services.
- Framework remains dispatcher-agnostic.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Validate templating logic and metadata interpolation.
- Test throttling policies for various event frequencies.
- Ensure channel senders handle authentication/configuration errors gracefully.

### Integration tests
- Configure sample alert channels, trigger events, and verify notifications reach targets with correct payloads.
- Simulate channel failures to ensure retries/backoff and logging occur.
- Expose alert diagnostics endpoints showing recent activity/throttling.

### Feature tests
- In OmniRelay.FeatureTests, trigger peer/transport failures to confirm alerts fire via the new framework.
- Validate operator workflows for enabling/disabling alert channels dynamically.

### Hyperscale Feature Tests
- Under OmniRelay.HyperscaleFeatureTests, generate high volumes of alerts to test throttling and channel scalability.
- Simulate simultaneous failures across clusters to ensure alert deduplication works.

## References
- Existing alert integrations (docs + runtime hooks) under dispatcher diagnostics.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
