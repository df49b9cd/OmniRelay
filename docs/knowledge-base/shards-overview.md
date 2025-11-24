# Shard Knowledge Base

## Domain Building Blocks
- **Records + DTOs**: `ShardRecord`/`ShardSummary` capture namespace, id, owner, strategy, capacity hint, checksum, version, leader, and tickets. `ShardControlPlaneMapper` keeps wire DTOs in sync.
- **Repository Contract**: `IShardRepository` must support point reads, namespace listings, optimistic `UpsertAsync`, diff streaming, and paged queries. The control plane relies on diff streams to emit watch events.
- **Filtering & Paging**: `ShardQueryCursor` encodes `namespace|shard` as Base64, `ShardQueryOptions` caps page size at 500, and `ShardFilter` applies namespace/owner/status/search constraints plus cursor rehydration.

## Control Plane & APIs
- **ShardControlPlaneService** wires repositories + hashing via Hugo result pipelines: `ListAsync`/`DiffAsync`/`SimulateAsync` now return `Result<T>` instead of throwing, `WatchAsync` streams `Result<ShardRecordDiff>`, and hashing validation failures surface as `shards.hashing.*`/`shards.control.*` codes that map to HTTP 4xx / gRPC `InvalidArgument`/`NotFound`.
- **HTTP diagnostics**: `ShardDiagnosticsEndpointExtensions` register `/control/shards`, `/control/shards/diff`, `/control/shards/watch` (SSE, resume tokens), and `/control/shards/simulate`, enforcing `mesh.read`/`mesh.operate` scopes and serializing through `ShardDiagnosticsJsonContext`.
- **gRPC service**: `ShardControlGrpcService` mirrors HTTP features, mapping domain summaries/history/assignments to `OmniRelay.Mesh.Control.V1` types, handling resume tokens, and throwing `PermissionDenied` if the `x-mesh-scope` header lacks the required claims.

## Repository Implementations
- **RelationalShardStore** (SQLite/Postgres compatible) ensures optimistic concurrency via expected versions, deterministic checksums, and audit history rows. Unit tests cover creation, updates with metadata, diff playback, stale-version failures, filtering, and cursor pagination.
- **ObjectStorageShardStore** filters documents client-side, implements `QueryAsync` with manual pagination + cursor math, and replays diffs from an in-memory queue—showing how non-relational stores can still satisfy the control-plane contract.

## CLI + Tooling Hooks
- `omnirelay mesh shards list|diff|simulate` construct shard URIs, normalize status filters against the domain enum, and render JSON (via `OmniRelayCliJsonContext`) or tabular output. Simulation parsing supports `--node nodeId[:weight[:region[:zone]]` tokens and enforces namespace + node requirements before issuing HTTP POSTs.
- CLI unit tests use `StubHttpMessageHandler` to inspect generated requests (query params, scope headers, payloads) and ensure invalid input short-circuits without touching the network.

## Test Coverage Snapshot
- **Unit**: `RelationalShardStoreTests` verify optimistic concurrency, diff history ordering, filtering, and pagination semantics.
- **Integration**: `ShardControlPlaneIntegrationTests` spin up `ShardControlPlaneTestHost`, seed SQLite shards, exercise list/diff/simulate endpoints with and without scope headers, and assert schema output via source-generated JSON contexts.
- **Feature**: `ShardControlFeatureTests` drive the CLI end-to-end against the host and assert exit codes plus stdout snippets for list/diff/simulate subcommands.
- **Hyperscale Feature**: `ShardControlHyperscaleFeatureTests` seed 2,000 shards, paginate in 500-record chunks while tracking cursors, and confirm totals to prove the diagnostics host + relational store can handle large namespaces.
