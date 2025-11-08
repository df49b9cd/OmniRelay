# PeerListCoordinator contention scenario

Validate that TTL-less traffic continues to make forward progress under sustained contention and that deadline expirations are surfaced distinctly from pool exhaustion.

## Topology

1. **Gateway** – run the OmniRelay dispatcher you plan to ship (HTTP or gRPC inbound). For quick staging, `samples/HybridRunner` can host both HTTP and gRPC endpoints.
2. **Outbound transport** – configure the target outbound (e.g. `GrpcOutbound`) with at least three peers so the `PeerListCoordinator` exercises its selection loop. Keep the peer chooser at the default `FewestPendingPeerChooser`.
3. **Backends** – stand up three gRPC echo services (for example `samples/StreamingAnalytics.Lab` workers) and intentionally cap their concurrency (e.g. limit ASP.NET Core `MaxConcurrentConnections` or add a server-side semaphore) so that peak load causes `TryAcquire` to fail periodically.

Example outbound snippet for `appsettings.Staging.json`:

```json
{
  "omniRelay": {
    "outbounds": {
      "analytics": {
        "transport": "grpc",
        "service": "trace.Analytics",
        "procedure": "trace.Analytics/Process",
        "endpoints": [
          "https://analytics-1.staging.corp:8443",
          "https://analytics-2.staging.corp:8443",
          "https://analytics-3.staging.corp:8443"
        ],
        "peerChooser": "fewest-pending"
      }
    }
  }
}
```

Deploy the gateway config without request-level TTLs or deadlines. This mimics production callers that do not set `meta.TimeToLive`.

## Load generation

Use the CLI benchmark to sustain contention. Leave `--ttl` and `--deadline` unset so the requests rely on the coordinator’s wait loop.

```bash
dotnet run --project src/OmniRelay.Cli \
    -- benchmark \
    --transport grpc \
    --service trace.Analytics \
    --procedure trace.Analytics/Process \
    --address https://gateway.staging.corp:8443 \
    --concurrency 400 \
    --rps 200 \
    --duration 10m \
    --warmup 30s \
    --encoding application/json \
    --body '{"payload":"load"}'
```

Recommendations:

- Run a second load phase with `--concurrency 800 --rps 400` to force sustained back-pressure.
- During a portion of the test, manually drain one backend or inject an artificial latency spike so peers transition between available/unavailable states while requests are pending.

## Observability

Collect the following during the run:

| Signal | Expectation |
| --- | --- |
| `omnirelay.peer.pool_exhausted` | Increases when all peers reject leases. Under TTL-less load you should see `ResourceExhausted` errors but **no** `DeadlineExceeded`. |
| `omnirelay.peer.lease_rejected` with `peer.rejection_reason="busy"` | Spikes when backends hit concurrency caps; verifies we are probing every peer. |
| `omnirelay.peer.lease.duration` | Should remain finite; look for long tails when peers recover. |
| `omnirelay.retry.*` counters (if retry middleware is enabled) | Ensure retries succeed without deadline bleed-through. |

For ad-hoc monitoring on a staging host:

```bash
dotnet-counters monitor --refresh-interval 1 \
    --counters OmniRelay.Core.Peers \
    --name OmniRelay.Dispatcher
```

Additionally, enable structured logs on the gateway and filter for `DeadlineExceeded` to confirm none are emitted during TTL-less runs. When you intentionally run with a tight deadline (e.g. `--deadline "$(date -u -Iseconds)"`) you should immediately observe `DeadlineExceeded` without inflating `omnirelay.peer.pool_exhausted`.

## Success criteria

1. With TTL-less load, request failures present as `ResourceExhausted` only after every available peer has rejected the lease. No `DeadlineExceeded` events should occur.
2. After removing back-pressure (e.g., ramping backends to full concurrency) the coordinator resumes leasing without requiring a process restart.
3. When you re-run the load with an explicit `--ttl 50ms`, `DeadlineExceeded` should be reported, distinguishing deadline violations from pool exhaustion.

Document the metric snapshots and any deviations before completing the rollout.
