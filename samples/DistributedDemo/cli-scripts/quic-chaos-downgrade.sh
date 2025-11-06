#!/usr/bin/env bash
set -euo pipefail

# QUIC downgrade chaos harness
# Scenarios:
#  - block-udp: Drop UDP/443 to force downgrade or failures
#  - h3-exact: Force client to HTTP/3 exact to observe ALPN mismatch behavior
#  - clear: Remove firewall rules
#
# Requirements: Linux with iptables or nft, sudo; OmniRelay CLI on PATH.

ACTION=${ACTION:-"block-udp"}         # block-udp|h3-exact|clear
TARGET_URL=${TARGET_URL:-"https://127.0.0.1:8443"}
DURATION=${DURATION:-"60s"}
RPS=${RPS:-"200"}
CONCURRENCY=${CONCURRENCY:-"32"}
FIREWALL=${FIREWALL:-"auto"}           # auto|iptables|nft
CHAIN=${CHAIN:-"INPUT"}                # for iptables (also applies to OUTPUT on loopback)
PORT=${PORT:-"443"}
IFACE=${IFACE:-"lo"}
PHASE=${PHASE:-"grpc"}                 # http|grpc

log() { echo "[$(date +'%F %T')] $*"; }

have_cmd() { command -v "$1" >/dev/null 2>&1; }

fw_add_udp_drop() {
  local dport="$1"
  if [[ "$FIREWALL" == "auto" ]]; then
    if have_cmd nft; then FIREWALL=nft; elif have_cmd iptables; then FIREWALL=iptables; else log "No nft or iptables found"; return 1; fi
  fi
  if [[ "$FIREWALL" == "nft" ]]; then
    log "Adding nft rule to drop udp dport ${dport}"
    sudo nft add table inet omnirelay 2>/dev/null || true
    sudo nft add chain inet omnirelay input { type filter hook input priority filter; } 2>/dev/null || true
    sudo nft add rule inet omnirelay input udp dport ${dport} drop || true
    # For loopback, also drop OUTPUT
    sudo nft add chain inet omnirelay output { type filter hook output priority filter; } 2>/dev/null || true
    sudo nft add rule inet omnirelay output udp dport ${dport} drop || true
  else
    log "Adding iptables rules to drop udp dport ${dport}"
    # INPUT and OUTPUT cover loopback client/server tests
    sudo iptables -I INPUT -p udp --dport ${dport} -j DROP || true
    sudo iptables -I OUTPUT -p udp --dport ${dport} -j DROP || true
  fi
}

fw_clear() {
  if [[ "$FIREWALL" == "auto" ]]; then
    if have_cmd nft; then FIREWALL=nft; elif have_cmd iptables; then FIREWALL=iptables; else return 0; fi
  fi
  if [[ "$FIREWALL" == "nft" ]]; then
    log "Clearing nft ruleset table inet omnirelay"
    sudo nft delete table inet omnirelay 2>/dev/null || true
  else
    log "Removing iptables DROP rules for udp dport ${PORT} (best-effort)"
    while sudo iptables -D INPUT -p udp --dport ${PORT} -j DROP 2>/dev/null; do :; done
    while sudo iptables -D OUTPUT -p udp --dport ${PORT} -j DROP 2>/dev/null; do :; done
  fi
}

bench_http3_exact() {
  # Force HTTP/3 exact using grpc/http handler semantics via bench flags
  if [[ "$PHASE" == "http" ]]; then
    log "Running HTTP client with HTTP/3 exact (expected failure if server doesn't support h3)"
    omnirelay bench http \
      --url "$TARGET_URL" \
      --http3 \
      --duration "$DURATION" \
      --rps "$RPS" \
      --concurrency "$CONCURRENCY" \
      --procedure ping \
      --encoding protobuf || true
  else
    log "Running gRPC client with HTTP/3 exact (expected failure if server doesn't support h3)"
    # grpc bench will request h3 (server side mismatch will lead to errors)
    omnirelay bench grpc \
      --address "$TARGET_URL" \
      --grpc-http3 \
      --duration "$DURATION" \
      --rps "$RPS" \
      --concurrency "$CONCURRENCY" \
      --service test \
      --procedure ping \
      --encoding protobuf || true
  fi
}

bench_normal() {
  if [[ "$PHASE" == "http" ]]; then
    omnirelay bench http \
      --url "$TARGET_URL" \
      --http3 \
      --duration "$DURATION" \
      --rps "$RPS" \
      --concurrency "$CONCURRENCY" \
      --procedure ping \
      --encoding protobuf || true
  else
    omnirelay bench grpc \
      --address "$TARGET_URL" \
      --grpc-http3 \
      --duration "$DURATION" \
      --rps "$RPS" \
      --concurrency "$CONCURRENCY" \
      --service test \
      --procedure ping \
      --encoding protobuf || true
  fi
}

main() {
  case "$ACTION" in
    block-udp)
      log "Blocking UDP/${PORT} to induce downgrade"
      fw_add_udp_drop "$PORT"
      bench_normal
      ;;
    h3-exact)
      log "Forcing HTTP/3 exact to observe ALPN mismatch behavior"
      bench_http3_exact
      ;;
    clear)
      fw_clear
      ;;
    *)
      echo "Unknown ACTION=${ACTION}. Use block-udp|h3-exact|clear" >&2; exit 1
      ;;
  esac
}

trap fw_clear EXIT
main "$@"
