# QUIC Soak and Performance Harness

This harness uses the OmniRelay CLI and Linux `tc netem` to simulate QUIC conditions (packet loss, latency, jitter, reordering, duplication, corruption, and optional rate shaping) and run HTTP/3 and gRPC-over-HTTP/3 soaks. It also supports connection churn and basic system-level stats collection. An experimental migration harness simulates IP changes (NAT rebinding) during a run.

## Prerequisites

- Linux host with sudo permissions
- `tc` (iproute2)
- OmniRelay CLI built and available on PATH (`omnirelay`)
- Target OmniRelay server listening on HTTPS with HTTP/3 enabled

## Quick start

```bash
export TARGET_URL=https://127.0.0.1:8443
export DURATION=300s
export RPS=200
export LOSS=1%
export DELAY=20ms
export JITTER=0ms
export IFACE=lo

bash samples/DistributedDemo/cli-scripts/quic-soak.sh
```

The script runs two phases by default (`PHASES="http,grpc"`):

1. HTTP/3 unary RPCs
2. gRPC unary RPCs over HTTP/3 (`--grpc-http3`)

Both honor `DURATION`, `RPS`, and `CONCURRENCY`. Netem is applied and cleaned up automatically when `APPLY_NETEM=1` (default).

## Scenarios

You can tune the impairments and load using environment variables (defaults shown):

```bash
IFACE=lo              # Interface to impair with tc netem
APPLY_NETEM=1         # Set to 0 to skip netem
LOSS=1%               # Random loss (supports advanced loss models too)
DELAY=20ms            # Base delay
JITTER=0ms            # Delay variance
REORDER=0%            # Packet reordering probability (requires DELAY)
REORDER_GAP=0         # Reorder gap (packets)
DUPLICATE=0%          # Packet duplication
CORRUPT=0%            # Packet corruption
RATE=                 # Optional rate limit (e.g., 100Mbit)
LIMIT_PKTS=10000      # Netem queue limit

CONCURRENCY=64        # Workers inside a single CLI process
CHURN_WORKERS=1       # Parallel CLI processes
CHURN_CYCLES=0        # Split run into N short iterations to force new connections
CHURN_SLEEP=2s        # Sleep between cycles

PHASES=http,grpc      # Which phases to run
COLLECT_STATS=1       # Collect basic system stats during the soak
STATS_INTERVAL=1      # Stats sampling interval (seconds)
OUT_DIR=./quic-soak-logs
```

Examples:

1) Reordering test with jitter:

```bash
REORDER=25% REORDER_GAP=5 DELAY=10ms JITTER=5ms bash samples/DistributedDemo/cli-scripts/quic-soak.sh
```

1) Packet loss and duplication:

```bash
LOSS=0.5% DUPLICATE=0.1% bash samples/DistributedDemo/cli-scripts/quic-soak.sh
```

1) High connection churn (spin 8 parallel workers with short runs):

```bash
CHURN_WORKERS=8 CHURN_CYCLES=10 DURATION=120s RPS=400 CONCURRENCY=64 bash samples/DistributedDemo/cli-scripts/quic-soak.sh
```

## Metrics and dashboards

- The script writes phase logs to `OUT_DIR` and collects basic host stats (`ss`, UDP socket counts, `/proc/net/snmp`).
- Combine with the provided Prometheus and Grafana artifacts (`samples/DistributedDemo/grafana/dashboards/omnirelay-quic-observability.json`, `samples/DistributedDemo/quic-alerts.yml`) to observe:
  - QUIC handshake RTT, per-protocol request rates, fallback rates.
  - Error rates and transport-level failures.
  - Connection churn and UDP socket dynamics.

If you have Prometheus available, we recommend capturing a metrics range before, during, and after the soak to establish baselines.

## Safety

- `tc netem` requires root and affects the chosen interface globally. Prefer using a dedicated test host or interface.
- Always run `tc qdisc del dev $IFACE root` after tests (the script traps EXIT and cleans up by default).
- Verify your server listens on the correct address/port (for the default examples: `https://127.0.0.1:8443`).

## Experimental: IP migration / NAT rebinding harness

QUIC supports client migration (surviving source IP/port changes). To exercise this, an experimental helper script uses a Linux network namespace and veth pair to change the client's source IP mid-run.

Script: `samples/DistributedDemo/cli-scripts/quic-migration-soak.sh`

Requirements:

- Linux root privileges
- Server must bind to a non-loopback address (e.g., `0.0.0.0`) and be reachable at the host-side veth IP

Usage:

```bash
sudo TARGET_URL=https://10.200.1.1:8443 \
    DURATION=120s MIGRATE_AFTER=45 \
    PHASE=grpc \
    bash samples/DistributedDemo/cli-scripts/quic-migration-soak.sh
```

What it does:

- Creates a netns and veth pair (`veth-host` <-> `veth-ns`) with host IP `10.200.1.1/24`.
- Runs the benchmark from the netns targeting the host IP.
- After `MIGRATE_AFTER` seconds, adds a secondary IP and changes the default route to prefer it, then removes the original IP.
- Observes whether connections continue successfully across the migration window.

Notes:

- MsQuic supports NAT rebinding/migration; ensure keep-alives are configured appropriately for your deployment. See Microsoft MsQuic Deployment docs (Client Migration).
- Adjust IPs, interface names, and target URLs as needed for your environment.


