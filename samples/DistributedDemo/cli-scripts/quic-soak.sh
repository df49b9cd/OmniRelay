#!/usr/bin/env bash
set -euo pipefail

# QUIC soak/perf harness using OmniRelay CLI and Linux tc netem for impairment injection.
# Requires: Linux with sudo, tc, bash; OmniRelay CLI built and on PATH (omnirelay)

# Core configuration (env overridable)
TARGET_URL=${TARGET_URL:-"https://127.0.0.1:8443"}
DURATION=${DURATION:-"300s"}
RPS=${RPS:-"200"}
CONCURRENCY=${CONCURRENCY:-"64"}
PHASES=${PHASES:-"http,grpc"}   # comma-separated: http,grpc

# Netem configuration
IFACE=${IFACE:-"lo"}
APPLY_NETEM=${APPLY_NETEM:-"1"}
LOSS=${LOSS:-"1%"}                 # e.g. 0.5%
DELAY=${DELAY:-"20ms"}             # base delay
JITTER=${JITTER:-"0ms"}            # delay variation
REORDER=${REORDER:-"0%"}           # e.g. 25%
REORDER_GAP=${REORDER_GAP:-"0"}    # packets gap for reorder
DUPLICATE=${DUPLICATE:-"0%"}
CORRUPT=${CORRUPT:-"0%"}
RATE=${RATE:-""}                   # e.g. 100Mbit
LIMIT_PKTS=${LIMIT_PKTS:-"10000"}

# Connection churn
CHURN_WORKERS=${CHURN_WORKERS:-"1"}    # number of parallel bench processes
CHURN_CYCLES=${CHURN_CYCLES:-"0"}      # number of short cycles to force new connections (0 = single run)
CHURN_SLEEP=${CHURN_SLEEP:-"2s"}       # sleep between cycles

# Basic system stats collection
COLLECT_STATS=${COLLECT_STATS:-"1"}
STATS_INTERVAL=${STATS_INTERVAL:-"1"}  # seconds
OUT_DIR=${OUT_DIR:-"./quic-soak-logs"}

log() { echo "[$(date +'%F %T')] $*"; }

apply_netem() {
  if [[ "${APPLY_NETEM}" != "1" ]]; then
    log "Skipping netem application (APPLY_NETEM=${APPLY_NETEM})."
    return
  fi
  log "Applying tc netem on ${IFACE} (limit=${LIMIT_PKTS}, loss=${LOSS}, delay=${DELAY} ${JITTER}, reorder=${REORDER} gap=${REORDER_GAP}, duplicate=${DUPLICATE}, corrupt=${CORRUPT}${RATE:+, rate=${RATE}})"
  sudo tc qdisc del dev "$IFACE" root || true
  # Build netem command
  NETEM=(tc qdisc add dev "$IFACE" root netem limit "$LIMIT_PKTS")
  [[ -n "${DELAY}" || -n "${JITTER}" ]] && NETEM+=(delay "$DELAY" ${JITTER:+"$JITTER"})
  [[ -n "${LOSS}" ]] && NETEM+=(loss "$LOSS")
  [[ -n "${DUPLICATE}" && "${DUPLICATE}" != "0%" ]] && NETEM+=(duplicate "$DUPLICATE")
  [[ -n "${CORRUPT}" && "${CORRUPT}" != "0%" ]] && NETEM+=(corrupt "$CORRUPT")
  if [[ -n "${REORDER}" && "${REORDER}" != "0%" ]]; then
    NETEM+=(reorder "$REORDER")
    [[ "${REORDER_GAP}" != "0" ]] && NETEM+=(gap "$REORDER_GAP")
  fi
  sudo "${NETEM[@]}"
  if [[ -n "${RATE}" ]]; then
    # nest TBF/ingress rate limiting if requested (best-effort)
    sudo tc qdisc add dev "$IFACE" parent 1:1 handle 10: tbf rate "$RATE" burst 32kbit latency 400ms || true
  fi
}

clear_netem() {
  if [[ "${APPLY_NETEM}" != "1" ]]; then return; fi
  log "Cleaning up tc netem on ${IFACE}"
  sudo tc qdisc del dev "$IFACE" root || true
}

collect_stats() {
  mkdir -p "$OUT_DIR"
  local prefix="$OUT_DIR/$(date +'%Y%m%d-%H%M%S')"
  log "Collecting stats every ${STATS_INTERVAL}s to ${prefix}.*"
  # ss summary and UDP socket counts
  while true; do
    echo "==== $(date +'%T') ====\n$(ss -s)" >>"${prefix}.ss" 2>&1 || true
    ss -A udp -a | wc -l >>"${prefix}.udp_sockets" 2>&1 || true
    cat /proc/net/snmp | grep -E '^Udp:' | tail -n1 >>"${prefix}.snmp.udp" 2>&1 || true
    sleep "${STATS_INTERVAL}" || break
  done &
  echo $! >"${prefix}.stats.pid"
}

stop_stats() {
  [[ -f "$OUT_DIR"/*.stats.pid ]] || return 0
  for f in "$OUT_DIR"/*.stats.pid; do
    kill "$(cat "$f" 2>/dev/null)" 2>/dev/null || true
    rm -f "$f" || true
  done
}

run_phase() {
  local phase="$1"; shift
  local duration="$1"; shift
  local log_file="$1"; shift
  local per_worker_duration="$duration"
  if [[ "$CHURN_CYCLES" -gt 0 ]]; then
    # Divide duration across cycles with small gaps
    local total_secs=$(( ${duration%s} ))
    local per_cycle=$(( total_secs / CHURN_CYCLES ))
    per_worker_duration="${per_cycle}s"
  fi
  log "Running phase=${phase} duration=${duration} rps=${RPS} conc=${CONCURRENCY} workers=${CHURN_WORKERS} cycles=${CHURN_CYCLES}"

  local run_once() {
    if [[ "$phase" == "http" ]]; then
      omnirelay bench http \
        --url "$TARGET_URL" \
        --http3 \
        --duration "$per_worker_duration" \
        --rps "$RPS" \
        --concurrency "$CONCURRENCY" \
        --procedure ping \
        --encoding protobuf
    else
      omnirelay bench grpc \
        --address "$TARGET_URL" \
        --grpc-http3 \
        --duration "$per_worker_duration" \
        --rps "$RPS" \
        --concurrency "$CONCURRENCY" \
        --service test \
        --procedure ping \
        --encoding protobuf
    fi
  }

  # fan out workers
  if [[ "$CHURN_WORKERS" -gt 1 || "$CHURN_CYCLES" -gt 0 ]]; then
    {
      for w in $(seq 1 "$CHURN_WORKERS"); do
        (
          if [[ "$CHURN_CYCLES" -gt 0 ]]; then
            for i in $(seq 1 "$CHURN_CYCLES"); do
              run_once || true
              sleep "$CHURN_SLEEP" || true
            done
          else
            run_once || true
          fi
        ) &
      done
      wait
    } |& tee -a "$log_file"
  else
    run_once |& tee -a "$log_file"
  fi
}

main() {
  apply_netem
  if [[ "${COLLECT_STATS}" == "1" ]]; then collect_stats; fi

  IFS=',' read -ra phases <<<"${PHASES}"
  for p in "${phases[@]}"; do
    run_phase "$p" "$DURATION" "${OUT_DIR}/soak-${p}.log" || true
  done

  stop_stats || true
  clear_netem || true
}

trap clear_netem EXIT
main "$@"
