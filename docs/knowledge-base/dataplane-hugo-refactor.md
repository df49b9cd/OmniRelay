# OmniRelay.DataPlane Hugo pipelines

- DataPlane streaming paths now use Hugo result streaming helpers (`Result.MapStreamAsync` + `TapSuccessEachAsync`/`TapFailureEachAsync`) so channel consumers stop on the first failure/cancellation and propagate completion through transport calls without throwing exceptions.
- Decode/encode steps must return `Result<T>`; map transport-specific context with `OmniRelayErrors.ToResult(...)` so status and metadata remain normalized for diagnostics and retries.
- Prefer `ValueTask` for hot-path handlers (HTTP outbound response parsing, streaming decode) and keep delegates `ValueTask<Result<T>>` friendly to avoid extra `Task` allocations in Native AOT builds.
- When consuming channel readers, rely on Hugo `ReadAllAsync` + result streaming rather than manual enumerator loops; attach failure taps to complete/tear down channels to prevent deadlocks.
- Keep the pattern central: validate metadata with `Ensure`, wrap async work with `ThenValueTaskAsync`, and avoid exception-based flow in business logic—surface errors as `Error` and `Result<T>` only. 
- Per-item telemetry: use `TapSuccessEachAsync`/`TapFailureEachAsync` to increment metrics counters on every frame/message (`omnirelay.http.server_stream.response_messages`, `omnirelay.http.duplex.{request|response}_messages`, `omnirelay.grpc.server.{client_stream|duplex}.{request|response}_messages`) so observability matches the Result pipeline and no exceptions leak hot paths.

## HTTP duplex (WebSocket) notes
- `HttpDuplexProtocol.ReceiveFrameAsync` currently throws `InvalidOperationException` for oversized frames, forcing callers to rely on `try/catch` in hot loops. Move this to a `ValueTask<Result<Frame>>` return so oversize/close conditions flow through Hugo result pipelines without throwing.
- `HttpDuplexStreamTransportCall` and `HttpInbound` both wrap framing with `Result.TryAsync` and catch `InvalidOperationException` to translate into `Error`. Replace these with direct `Result` propagation and shared helpers to eliminate duplicate `NormalizeTransportException` and reduce allocation-heavy exception paths.
- Ensure frame copy/backpressure behavior stays in Hugo channels: receive pump should emit `Result<Frame>` through a bounded channel and stop on the first failure, completing request/response writers with the captured error.
- Standardize error shapes for duplex: use `OmniRelayErrorAdapter.FromStatus(ResourceExhausted|Cancelled|Internal, ...)` rather than ad-hoc exceptions; serialize errors with `HttpDuplexProtocol.CreateErrorPayload` only after they are already `Result` failures.
- Keep write timeouts explicit: wrap `SendFrameAsync` with timeout-aware `Result` helpers instead of surfacing `TimeoutException`; prefer `Result.Fail(Error.DeadlineExceeded(...))` to keep business logic exception-free.
