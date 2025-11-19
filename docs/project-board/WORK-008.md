# WORK-008 – Configuration Reload & Watcher Services

## Goal
Provide a reusable configuration watcher/service that monitors JSON/environment/secret sources, validates updates, and applies them safely across OmniRelay transports and MeshKit modules without restarts.

## Scope
- Extract reload logic (file watchers, debounce, validation-before-apply) from existing dispatcher hosting code.
- Offer APIs to register configuration sections with validation callbacks and rollback semantics.
- Surface reload status/events via diagnostics endpoints and CLI commands.
- Support multiple sources (files, env, secret providers) and integrate with MeshKit + OmniRelay DI pipelines.

## Requirements
1. **Safe reloads** – Validate new config snapshots before applying; rollback on failure with clear logging and metrics.
2. **Debounce/throttling** – Coalesce rapid file changes; allow configurable delays per section.
3. **Multi-source watching** – Monitor JSON files, env vars, optional remote stores; integrate with secret providers for TLS/credentials.
4. **Observability** – Emit metrics/logs for reloads, failures, applied sections, and expose via diagnostics endpoints.
5. **AOT** – Watcher services must function in native AOT hosts.

## Deliverables
- Shared watcher service/library, tests, docs.
- Wiring updates for OmniRelay + MeshKit hosts.

## Acceptance Criteria
- OmniRelay/MeshKit hosts can enable hot reload for documented sections; invalid updates roll back with actionable errors.
- Diagnostics/CLI show reload history and status.
- Native AOT tests pass per WORK-002..WORK-005.

## Testing Strategy
- Unit: debounce logic, validation callbacks, rollback behavior, multiple section handling.
- Integration: modify config while hosts run, confirm reload, rollback, diagnostics output.
- Feature: operator workflows toggling settings (rate limits, policies) via reload.
- Hyperscale: large deployments rolling config updates without thrash.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`