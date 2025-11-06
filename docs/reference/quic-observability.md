# QUIC and Protocol Observability

This guide explains how OmniRelay surfaces QUIC/HTTP/3 observability signals and how to interpret them during incidents.

## What we emit

- Structured logs from MsQuic/Kestrel EventSources via `QuicKestrelEventBridge`:
  - Handshake failures (ALPN mismatch, TLS errors)
  - Connection migration and path validation events
  - Congestion indicators (loss/retransmits)
- Protocol-level HTTP metrics from the HTTP inbound (`OmniRelay.Transport.Http` meter):
  - `omnirelay.http.requests.started` (Counter)
  - `omnirelay.http.requests.completed` (Counter)
  - `omnirelay.http.request.duration` (Histogram, ms)
  - Tags include `rpc.service`, `rpc.procedure`, `http.request.method`, `rpc.protocol`, `network.protocol.name/version`, `network.transport`.
- gRPC unary metrics (`OmniRelay.Transport.Grpc` meter):
- Tracing attributes on server spans:
  - `rpc.protocol`, `network.protocol.name`, `network.protocol.version`, `network.transport`
  - HTTP inbounds annotate the current server Activity; gRPC server spans include the same tags.
  - Exporters: validated with OpenTelemetry .NET SDK (gRPC OTLP, Prometheus metrics). Attributes use conventional semantic keys and do not introduce schema drift.
  - `yarpcore.grpc.server.unary.duration`, `yarpcore.grpc.client.unary.duration` (Histogram, ms)
  - Tags include `rpc.protocol` and protocol metadata on the server side.

## Dashboards

Import `samples/DistributedDemo/grafana/dashboards/omnirelay-quic-observability.json` in Grafana. Key charts:

- Requests completed by protocol: confirms HTTP/3 adoption and fallback rates.
- Request p95 latency split by protocol: compare HTTP/3 vs HTTP/2.
- Client fallback trends (HTTP + gRPC): aggregates `omnirelay_http_client_fallbacks_total` and `omnirelay_grpc_client_fallbacks_total` rates.

For the demo stack, Prometheus scrapes each service’s `/omnirelay/metrics`. See `samples/DistributedDemo/prometheus.yml`.

## Alerts

`samples/DistributedDemo/quic-alerts.yml` contains example Prometheus alert rules:

- HighHTTP3FallbackRate – warns when >20% of requests are not served over HTTP/3.
- HTTP3LatencyRegressionP95 – warns when p95 latency for HTTP/3 exceeds 250 ms for 10 minutes.

Tune thresholds per service SLOs.

## Troubleshooting flow

1. Check fallback rate panel. If elevated, verify:
   - UDP/443 open path to service.
   - `alt-svc` is present at the edge and not stripped.
   - Kestrel listeners advertise `Http1AndHttp2AndHttp3` and HTTPS with TLS 1.3.
2. Inspect structured logs for `handshake_failure` from `QuicKestrelEventBridge`:
   - ALPN errors usually indicate address or protocol mismatch.
   - TLS errors indicate certificate or cipher issues.
3. Compare p95 latency for HTTP/3 vs HTTP/2 to isolate QUIC-specific congestion.
4. If mobile-like IP migration occurs, expect `migration` events; elevated counts may indicate flaky networks.
5. Use tracing views to confirm protocol attributes on spans; ensure HTTP/3 spans show `network.transport=quic` and correct `network.protocol.version`.

## Operator tips

- Correlate logs with request-scoped metadata (`rpc.service`, `rpc.procedure`, `rpc.request_id`).
- When disabling HTTP/3 for mitigation, watch fallback rates drop and error rates stabilize before closing incidents.
- Use `curl --http3` and browser devtools to validate end-to-end upgrades during rollouts.
