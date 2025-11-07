# Streaming Analytics Lab

A runnable streaming playground that hosts OmniRelay server, client, and duplex procedures side-by-side. The lab focuses on real-time telemetry flows—ticker subscriptions over JSON, ESG metric aggregation via client streaming, and insight collaboration over duplex streams. Typed clients inside the sample connect through the OmniRelay dispatcher itself, showing how to reuse codecs and middleware for both inbound and outbound traffic.

## Highlights

| Procedure | Shape | Codec | Description |
| --- | --- | --- | --- |
| `marketdata::ticker-stream` | Server stream | JSON (`JsonCodec`) | Emits ESG-friendly ticker updates at caller-specified intervals. |
| `metrics::aggregate` | Client stream | Protobuf (`MetricSample` / `MetricAck`) | Accepts a stream of portfolio metrics and responds with an aggregate summary. |
| `insights::collab` | Duplex | Protobuf (`InsightRequest` / `InsightSignal`) | Analysts push sentiment updates while the server sends collaborative signals back in real time. |

## Run the sample

```bash
dotnet run --project samples/StreamingAnalytics.Lab
```

Outputs:

- OmniRelay gRPC inbound: `http://127.0.0.1:7190`
- Internal demo clients connect through loopback gRPC outbounds so the dispatcher exercises both inbound and outbound streaming middleware.

Press `Ctrl+C` to stop the dispatcher and the demo clients.

## Observability helpers

Use the CLI to inspect the dispatcher or watch metrics while the demo is running:

```bash
# Inspect streaming procedures, middleware, and diagnostics
omnirelay introspect --url http://127.0.0.1:7190/omnirelay/introspect --format text

# Hit health/readiness if you expose them through an HTTP inbound in your environment
# (the sample focuses on gRPC, but the dispatcher still publishes /omnirelay endpoints)
omnirelay request \
  --transport http \
  --url http://127.0.0.1:7190/omnirelay/introspect \
  --service samples.streaming-lab \
  --procedure omnirelay::introspect \
  --encoding application/json
```

> The built-in demo clients already stream data across the dispatcher. To test with your own clients, point an OmniRelay or gRPC caller at `http://127.0.0.1:7190` and reuse the JSON/Protobuf contracts included in the sample (`TickerSubscription`, `TickerUpdate`, and `Protos/analytics.proto`).

## Code tour

- `Program.cs` builds the dispatcher with a gRPC inbound, registers JSON + Protobuf codecs, wires streaming handlers, and starts three demo clients (`StreamClient`, `ClientStreamClient`, `DuplexStreamClient`).
- `StreamingHandlers` shows server, client, and duplex handlers:
  - JSON handler manually encodes/decodes ticker updates.
  - Protobuf handlers use `ProtobufCallAdapters` to expose typed streaming contexts.
- `StreamingDemo` demonstrates backpressure-aware clients that loop until cancellation, printing ticker updates, aggregation responses, and duplex collaboration signals.
- `Protos/analytics.proto` defines the Protobuf contracts for metrics and insight collaboration, compiled at build time via `Grpc.Tools`.
