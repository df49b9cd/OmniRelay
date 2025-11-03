# Polymer Error Handling

Guidance for surfacing and interpreting errors consistently across transports while maintaining parity with `yarpc-go`.

## Status & Metadata

- `PolymerException` wraps failures with a canonical `PolymerStatusCode`, the original `Hugo.Error`, and the transport that surfaced the issue.
- `PolymerErrorAdapter` annotates errors with:
  - `polymer.status`: string representation of the status code.
  - `polymer.faultType`: `Client` or `Server` classification (when known).
  - `polymer.retryable`: boolean hint to retry middleware and call sites.
  - `polymer.transport`: transport identifier (`http`, `grpc`, …).
- Helpers in `PolymerErrors` provide structured handling:
  - `PolymerErrors.FromException` → `PolymerException`.
  - `PolymerErrors.IsStatus` / `TryGetStatus`.
  - `PolymerErrors.GetFaultType` for quick classification.
  - `PolymerErrors.IsRetryable` to align with outbound retry policy.

## ASP.NET Core (HTTP)

Use `PolymerExceptionFilter` to normalize exceptions thrown by controllers, Razor pages, and minimal API endpoints:

```csharp
builder.Services.AddControllers(options =>
{
    options.AddPolymerExceptionFilter(); // transport defaults to "http"
});
```

Effects:

- Converts unhandled exceptions into `PolymerException`.
- Writes canonical headers (`Rpc-Status`, `Rpc-Error-Code`, `Rpc-Error-Message`, `Rpc-Transport`).
- Serializes the error payload (`message`, `status`, `code`, `metadata`) so HTTP clients receive the same shape as Polymer inbounds.

For minimal APIs, register the filter on the shared `MvcOptions` or wrap handlers with a try/catch that calls `PolymerErrors.FromException`.

## gRPC Services

Add `GrpcExceptionAdapterInterceptor` to the server to map thrown exceptions into canonical RPC trailers:

```csharp
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcExceptionAdapterInterceptor>();
});
```

Benefits:

- Non-`RpcException` failures become `RpcException`s whose status matches `PolymerStatusCode`.
- Trailers include Polymer metadata (`polymer-status`, `polymer-error-code`, `polymer-transport`, fault/retry hints).
- Existing middleware and diagnostics that rely on trailers stay aligned with the HTTP transport.

If you already throw `RpcException` with Polymer trailers, the interceptor leaves the exception untouched.

## Client Patterns

- Always wrap outbound faults with `PolymerErrors.FromException` (the retry middleware performs this automatically).
- Use `PolymerErrors.IsRetryable(error)` before manual retries.
- Inspect `PolymerErrors.GetFaultType(exception)` for client/server attribution in logs.
- When emitting structured logs, include `PolymerException.Error.Metadata` to preserve fault details.

## Testing & Diagnostics

- Unit tests can assert metadata via `PolymerErrorAdapter.FaultMetadataKey` / `RetryableMetadataKey`.
- HTTP integration tests should verify the response headers/JSON mirror the filter output.
- gRPC tests should inspect response trailers for `polymer-status` and `polymer.retryable` to confirm adapter wiring.

## Migration Checklist

1. Register `PolymerExceptionFilter` for ASP.NET Core entry points.
2. Register `GrpcExceptionAdapterInterceptor` for gRPC services.
3. Ensure custom middleware rethrows `PolymerException` or wraps via `PolymerErrors.FromException`.
4. Update documentation/tooling references to include the new adapters.  
   (The parity backlog tracks this under **Error Model Parity → Error Helpers**.)
