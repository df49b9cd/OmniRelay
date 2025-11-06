# QUIC Downgrade Chaos Harness

Use this helper to simulate downgrade conditions and validate resiliency and error messaging.

Scenarios covered:

- Block UDP/443 to force downgrade from HTTP/3 to HTTP/2/1.1 or trigger failures.
- Force client to HTTP/3 (exact) to observe ALPN mismatch behavior when server is HTTP/2-only.
- Clear firewall rules after tests.

Script: `samples/DistributedDemo/cli-scripts/quic-chaos-downgrade.sh`

## Prerequisites

- Linux host with sudo privileges
- iptables or nftables installed
- OmniRelay CLI on PATH (`omnirelay`)
- Target server listening on HTTPS at `TARGET_URL`

## Usage

Block UDP/443 and run a normal HTTP/3 benchmark (should downgrade or fail):

```bash
ACTION=block-udp TARGET_URL=https://127.0.0.1:8443 \
  DURATION=60s RPS=200 CONCURRENCY=32 PHASE=grpc \
  bash samples/DistributedDemo/cli-scripts/quic-chaos-downgrade.sh
```

Force HTTP/3 exact to validate ALPN mismatch behavior (server h2-only):

```bash
ACTION=h3-exact TARGET_URL=https://127.0.0.1:8443 \
  DURATION=30s RPS=100 CONCURRENCY=16 PHASE=http \
  bash samples/DistributedDemo/cli-scripts/quic-chaos-downgrade.sh
```

Clear firewall rules:

```bash
ACTION=clear bash samples/DistributedDemo/cli-scripts/quic-chaos-downgrade.sh
```

## How it works

- UDP block: Adds simple nftables or iptables rules to drop UDP traffic on port 443 (both INPUT and OUTPUT chains for loopback testing). This prevents QUIC handshakes and should force the client to continue over HTTP/2 (if enabled) or fail depending on policy.
- HTTP/3 exact: Runs the benchmark requesting HTTP/3; if server is HTTP/2-only, calls are expected to fail with actionable errors.

## What to observe

- Client output (errors/timeouts): Ensure messages are clear and indicate protocol negotiaton issues where applicable.
- Server logs and metrics: Verify fallback rates increase and QUIC handshake failures appear with appropriate counters.
- Traces: Confirm `rpc.protocol` reflects HTTP/2 after downgrade.

## Safety

- Always run `ACTION=clear` after tests (script clears on EXIT by default).
- Be cautious on shared hosts: firewall changes are global. Prefer isolated test environments.

## References

- nftables: Red Hat docs “Getting started with nftables”.
- iptables: Standard usage to drop UDP ports.
- curl --http3: See curl docs if you use curl for quick checks.
