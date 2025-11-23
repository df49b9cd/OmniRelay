# OmniRelay.DataPlane Hugo pipelines

- DataPlane streaming paths now use Hugo result streaming helpers (`Result.MapStreamAsync` + `TapSuccessEachAsync`/`TapFailureEachAsync`) so channel consumers stop on the first failure/cancellation and propagate completion through transport calls without throwing exceptions.
- Decode/encode steps must return `Result<T>`; map transport-specific context with `OmniRelayErrors.ToResult(...)` so status and metadata remain normalized for diagnostics and retries.
- Prefer `ValueTask` for hot-path handlers (HTTP outbound response parsing, streaming decode) and keep delegates `ValueTask<Result<T>>` friendly to avoid extra `Task` allocations in Native AOT builds.
- When consuming channel readers, rely on Hugo `ReadAllAsync` + result streaming rather than manual enumerator loops; attach failure taps to complete/tear down channels to prevent deadlocks.
- Keep the pattern central: validate metadata with `Ensure`, wrap async work with `ThenValueTaskAsync`, and avoid exception-based flow in business logic—surface errors as `Error` and `Result<T>` only. 
- Per-item telemetry: use `TapSuccessEachAsync`/`TapFailureEachAsync` to increment metrics counters on every frame/message (`omnirelay.http.server_stream.response_messages`, `omnirelay.http.duplex.{request|response}_messages`, `omnirelay.grpc.server.{client_stream|duplex}.{request|response}_messages`) so observability matches the Result pipeline and no exceptions leak hot paths.
