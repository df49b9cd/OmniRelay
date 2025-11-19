# WORK-010 – MeshKit.Shards APIs & Tooling

## Goal
Deliver MeshKit.Shards as the canonical control-plane module that exposes shard ownership data over HTTP/3 + gRPC and equips operators with CLI/scripting utilities, while OmniRelay stays transport-only.

## Scope
- Implement `/meshkit/shards` REST + gRPC endpoints with filtering (namespace, owner, status), cursor-based pagination, and optimistic concurrency metadata (etag + version vector).
- Provide shard watch streams (SSE + gRPC) powered by the MeshKit event bus with resume tokens and negotiated transport tracking.
- Wire `omnirelay mesh shards list/diff/simulate` commands to the MeshKit endpoints with JSON + table output plus dry-run validation inside MeshKit.
- Publish OpenAPI/Protobuf contracts, samples, and SDK snippets for third-party automation.

## Requirements
1. **Ownership source of truth** – MeshKit persists shard assignments (namespace strategy, owners, audit trail) and serves them via repository interfaces; OmniRelay only consumes results via standard transports.
2. **Security** – Enforce MeshKit RBAC scopes (`mesh.read`, `mesh.observe`, `mesh.operate`) and log/metric every access. CLI helpers must surface scope issues clearly.
3. **Performance** – Target P95 <200 ms for 1k shards per query and support at least 100 concurrent watch subscribers per node.
4. **Resilience** – Watch streams resume from last delivered version using resume tokens; HTTP/3 downgrades to HTTP/2 must be observable via telemetry.
5. **CLI UX** – Commands support `--namespace`, `--owner`, `--status`, `--json`, `--table`, and exit non-zero on validation/auth errors. Output contracts need golden tests.

## Deliverables
- MeshKit.Shards service (domain models, repositories, controllers) hosted via OmniRelay transport builders.
- Protobuf + OpenAPI contracts and docs describing filtering, pagination, watch semantics, and RBAC expectations.
- CLI verbs + integration tests hitting a live MeshKit.Shards fixture.
- Sample configs (MeshKit + samples/ResourceLease.MeshDemo) showing MeshKit.Shards wiring.

## Acceptance Criteria
- MeshKit.Shards delivers HTTP/3 + gRPC endpoints and SSE/gRPC watches that match documented contracts and telemetry expectations.
- CLI/SDK workflows operate end-to-end against MeshKit.Shards and honor RBAC scopes and downgrade telemetry.
- Resume tokens survive restarts and watchers catch up without missing updates.
- All transport/control binaries pass native AOT publish and the relevant test suites (unit, integration, feature, hyperscale) as per WORK-002..WORK-005.

## Testing Strategy
All test tiers must run against native AOT artifacts per WORK-002..WORK-005.

### Unit Tests
- Validate shard query/filter builders, pagination, resume token helpers, and RBAC validators.
- Cover serialization for snapshots, diff payloads, and watch envelopes with downgrade metadata.
- CLI golden tests for list/diff/simulate output (JSON + table) and error flows.

### Integration Tests
- Run MeshKit.Shards against relational/object stores to ensure queries, watches, and resume tokens behave under load and RBAC enforcement.
- Execute CLI verbs against the hosted service (HTTP/3 + forced HTTP/2) verifying telemetry, logging, and transport negotiation data.

### Feature Tests
- Feature harness: operator workflow listing shards, diffing versions, running simulations, and validating CLI/metric parity.

### Hyperscale Tests
- Hyperscale harness: thousands of shards/namespaces with rolling node churn to validate latency targets, watcher resilience, and CLI responsiveness.


## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`