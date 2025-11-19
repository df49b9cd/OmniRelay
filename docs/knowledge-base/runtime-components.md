# Runtime Components

## Dispatcher (`src/OmniRelay`)
- **Transports**: `Transport.Http` and `Transport.Grpc` expose unary, oneway, and streaming RPC shapes. HTTP inbound also serves `/omnirelay/introspect`, `/healthz`, `/readyz`.
- **Codecs**: JSON (`JsonCodec`), Protobuf, and raw codecs plug into dispatcher registrations; custom codecs implement encode/decode helpers returning `Result<T>`.
- **Middleware**: Logging, tracing (`RpcTracingMiddleware`), metrics, retry/deadline enforcement, panic recovery, rate limiting, chaos toggles, and peer circuit breakers all live under `Core/Middleware` and can be applied globally or per procedure.
- **Peer & routing**: Choosers (round-robin, fewest-pending, two-random-choice), peer list watchers, and sharding helpers (resource lease components, hashing strategies) sit under `Core/Peers` and `Core/Shards`.
- **ResourceLease mesh**: `ResourceLease*` contracts plus replicators (gRPC, SQLite, object storage) coordinate SafeTaskQueue workflows, failure drills, and deterministic recovery.

## Hashing & Shards (`src/OmniRelay/Core/Shards`)
- Hash strategies (rendezvous, ring, locality-aware) feed into `ShardControlPlaneService` for simulations and rebalancing.
- Repositories implement `IShardRepository` for storage-specific persistence (relational, object storage).

## Code generation
- `OmniRelay.Codegen.Protobuf` provides the `protoc-gen-omnirelay-csharp` plug-in.
- `OmniRelay.Codegen.Protobuf.Generator` is a Roslyn incremental generator that emits dispatcher/client glue at compile time.

## CLI helpers
- `OmniRelay.Cli` hosts config validation (`omnirelay config validate`), dispatcher introspection, benchmarking, scripting, node upgrade/drain flows, and the new `mesh shards *` commands feeding into shard diagnostics.