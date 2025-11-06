#!/usr/bin/env bash
set -euo pipefail

# Experimental: QUIC migration/nat-rebinding harness using a Linux network namespace.
# It sets up a veth pair between host and a netns, runs the benchmark inside
# the namespace to target the host IP, and mid-run switches the client's source IP.
# Requires: Linux root, iproute2 (ip, tc), OmniRelay CLI on PATH.

NS_NAME=${NS_NAME:-"qns1"}
HOST_IF=${HOST_IF:-"veth-host"}
NS_IF=${NS_IF:-"veth-ns"}
HOST_IP=${HOST_IP:-"10.200.1.1/24"}
NS_IP1=${NS_IP1:-"10.200.1.2/24"}
NS_IP2=${NS_IP2:-"10.200.1.3/24"}
TARGET_URL=${TARGET_URL:-"https://10.200.1.1:8443"}  # server must bind to 0.0.0.0/::
DURATION=${DURATION:-"120s"}
MIGRATE_AFTER=${MIGRATE_AFTER:-"45"}                # seconds
RPS=${RPS:-"200"}
CONCURRENCY=${CONCURRENCY:-"64"}
PHASE=${PHASE:-"grpc"}  # http|grpc

cleanup() {
  echo "Cleaning up namespace ${NS_NAME} and veths..."
  ip netns del "${NS_NAME}" 2>/dev/null || true
  ip link del "${HOST_IF}" 2>/dev/null || true
}

setup() {
  cleanup
  echo "Setting up veth pair ${HOST_IF}<->${NS_IF} and namespace ${NS_NAME}"
  ip netns add "${NS_NAME}"
  ip link add "${HOST_IF}" type veth peer name "${NS_IF}"
  ip link set "${NS_IF}" netns "${NS_NAME}"
  ip addr add "${HOST_IP}" dev "${HOST_IF}"
  ip link set "${HOST_IF}" up
  ip netns exec "${NS_NAME}" ip addr add "${NS_IP1}" dev "${NS_IF}"
  ip netns exec "${NS_NAME}" ip link set "${NS_IF}" up
  # default route via host
  HOST_IP_ADDR=${HOST_IP%/*}
  ip netns exec "${NS_NAME}" ip route add default via "${HOST_IP_ADDR}" dev "${NS_IF}"
}

bench_cmd() {
  if [[ "${PHASE}" == "http" ]]; then
    echo "omnirelay bench http --url '${TARGET_URL}' --http3 --duration '${DURATION}' --rps '${RPS}' --concurrency '${CONCURRENCY}' --procedure ping --encoding protobuf"
  else
    echo "omnirelay bench grpc --address '${TARGET_URL}' --grpc-http3 --duration '${DURATION}' --rps '${RPS}' --concurrency '${CONCURRENCY}' --service test --procedure ping --encoding protobuf"
  fi
}

migrate() {
  echo "[ns:${NS_NAME}] Performing IP migration to ${NS_IP2} after ${MIGRATE_AFTER}s..."
  sleep "${MIGRATE_AFTER}"
  ip netns exec "${NS_NAME}" ip addr add "${NS_IP2}" dev "${NS_IF}"
  # prefer new source IP for the default route
  NS_IP2_ADDR=${NS_IP2%/*}
  HOST_IP_ADDR=${HOST_IP%/*}
  ip netns exec "${NS_NAME}" ip route replace default via "${HOST_IP_ADDR}" dev "${NS_IF}" src "${NS_IP2_ADDR}"
  # remove old IP after a short grace period
  sleep 5
  ip netns exec "${NS_NAME}" ip addr del "${NS_IP1}" dev "${NS_IF}"
  echo "[ns:${NS_NAME}] Migration complete."
}

main() {
  if [[ $EUID -ne 0 ]]; then
    echo "This script must be run as root (sudo)." >&2; exit 1
  fi
  setup
  trap cleanup EXIT
  echo "Launching benchmark in namespace ${NS_NAME} targeting ${TARGET_URL}"
  # Start migration in background
  migrate &
  MIG_PID=$!
  # Run benchmark inside namespace
  CMD=$(bench_cmd)
  set +e
  ip netns exec "${NS_NAME}" bash -lc "$CMD" || true
  set -e
  wait "$MIG_PID" || true
}

main "$@"
