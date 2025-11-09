## TableLease dispatcher component

`TableLeaseDispatcherComponent` (under `src/OmniRelay/Dispatcher/TableLeaseDispatcher.cs`) hosts a dedicated `TaskQueue<TableLeaseWorkItem>` behind Hugo’s `SafeTaskQueueWrapper<T>` so every metadata node can expose a consistent lease surface. When constructed with a dispatcher it automatically registers the following JSON procedures (namespace defaults to `tablelease` and can be overridden via `TableLeaseDispatcherOptions.Namespace`):

| Procedure | Description |
| --- | --- |
| `tablelease::enqueue` | Accepts `TableLeaseEnqueueRequest` and appends the payload to the SafeTaskQueue. Returns `TableLeaseEnqueueResponse` with live pending/active stats. |
| `tablelease::lease` | Blocks until a lease is granted and returns `TableLeaseLeaseResponse` containing the payload, `SequenceId`, `Attempt`, and a `TableLeaseOwnershipHandle` (token = SequenceId, Attempt, LeaseId). Tokens back the ack operations below. |
| `tablelease::complete` | Completes the outstanding lease referenced by `TableLeaseOwnershipHandle`. |
| `tablelease::heartbeat` | Issues a heartbeat for the referenced lease without handing work to another node. |
| `tablelease::fail` | Fails or requeues the referenced lease with a structured `Error` derived from the request. |
| `tablelease::drain` | Calls `TaskQueue<T>.DrainPendingItemsAsync` and returns serialized `TableLeasePendingItemDto` records (payload + attempt/dead-letter metadata). |
| `tablelease::restore` | Rehydrates drained items by constructing `TaskQueuePendingItem<T>` instances and invoking `RestorePendingItemsAsync`. |

Each DTO lives alongside the component (`TableLeaseItemPayload`, `TableLeaseOwnershipHandle`, `TableLeaseErrorInfo`, etc.) and is encoded via the existing JSON dispatcher helpers. Pending/restore flows preserve `SequenceId`, `Attempt`, and prior ownership tokens so another node can replay the same work with its original fencing metadata.

Use `TableLeaseDispatcherOptions.QueueOptions` to align lease duration, heartbeat cadence, capacity, and backpressure thresholds with Lakeview’s SafeTaskQueue settings.

---

- Replicated stream layer: add an ordered broadcast mechanism (e.g., Raft-style log or deterministic sequencer) so every metadata node sees the same lease events with fencing tokens; wire this through streaming transports and include replay/dedup logic using deterministic coordination primitives (docs/reference/hugo-api-reference.md (lines 136-154), docs/reference/deterministic-coordination.md (lines 16-96)).

- Health + membership integration: extend the peer subsystem (src/OmniRelay/Core/Peers/*) with SafeTaskQueue heartbeats, disconnect callbacks, and membership gossip; feed status into lease ownership decisions and expose metrics/introspection showing node liveness and pending reassignments.

- Backpressure + flow control: connect TaskQueueBackpressureOptions callbacks (docs/reference/concurrency-primitives.md (lines 153-207)) to producer throttling in OmniRelay transports/middleware so slow consumers trigger upstream slowdown instead of unbounded buffering; add operator-visible gauges for high-watermark states.

- Deterministic recovery tooling: incorporate DeterministicGate, VersionGate, and DeterministicEffectStore (docs/reference/hugo-api-reference.md (lines 167-190), docs/reference/deterministic-coordination.md (lines 16-96)) into the TableLease workflow so failovers, reassignments, and compensations replay consistently; expose APIs/config to persist deterministic state in Lakeview’s chosen store.

- Security & identity propagation: create middleware that binds TLS client certs or auth tokens to RequestMeta.Caller/principal metadata on every lease message, ensuring traceability across replicated streams; document how Lakeview operators configure mTLS and audit trails.
