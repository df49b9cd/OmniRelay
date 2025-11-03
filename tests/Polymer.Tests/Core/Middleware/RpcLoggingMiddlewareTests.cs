using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polymer.Core;
using Polymer.Core.Middleware;
using Polymer.Core.Transport;
using Polymer.Errors;
using Polymer.Tests.Support;
using Xunit;
using static Hugo.Go;

namespace Polymer.Tests.Core.Middleware;

public sealed class RpcLoggingMiddlewareTests
{
    [Fact]
    public async Task UnaryInbound_Success_LogsCompletion()
    {
        var logger = new TestLogger<RpcLoggingMiddleware>();
        var middleware = new RpcLoggingMiddleware(logger);

        var requestMeta = new RequestMeta(service: "svc", procedure: "echo::call", encoding: "application/json", transport: "grpc");
        var request = new Request<ReadOnlyMemory<byte>>(requestMeta, ReadOnlyMemory<byte>.Empty);
        var response = Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty, new ResponseMeta(encoding: "application/json"));

        var result = await middleware.InvokeAsync(
            request,
            CancellationToken.None,
            (UnaryInboundDelegate)((req, token) => ValueTask.FromResult(Ok(response))));

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("inbound unary", entry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("svc", entry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("echo::call", entry.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnaryOutbound_Failure_LogsError()
    {
        var logger = new TestLogger<RpcLoggingMiddleware>();
        var middleware = new RpcLoggingMiddleware(logger);

        var requestMeta = new RequestMeta(service: "svc", procedure: "echo::call", transport: "grpc");
        var request = new Request<ReadOnlyMemory<byte>>(requestMeta, ReadOnlyMemory<byte>.Empty);
        var error = PolymerErrorAdapter.FromStatus(PolymerStatusCode.Internal, "boom", transport: "grpc");

        var result = await middleware.InvokeAsync(
            request,
            CancellationToken.None,
            (UnaryOutboundDelegate)((req, token) => ValueTask.FromResult(Err<Response<ReadOnlyMemory<byte>>>(error))));

        Assert.True(result.IsFailure);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.LogLevel);
        Assert.Contains("failed", entry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("boom", entry.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShouldLogRequestFalse_SkipsSuccessLog()
    {
        var logger = new TestLogger<RpcLoggingMiddleware>();
        var options = new RpcLoggingOptions
        {
            ShouldLogRequest = _ => false
        };
        var middleware = new RpcLoggingMiddleware(logger, options);

        var requestMeta = new RequestMeta(service: "svc", procedure: "echo::call", transport: "grpc");
        var request = new Request<ReadOnlyMemory<byte>>(requestMeta, ReadOnlyMemory<byte>.Empty);

        var result = await middleware.InvokeAsync(
            request,
            CancellationToken.None,
            (UnaryInboundDelegate)((req, token) => ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty)))));

        Assert.True(result.IsSuccess);
        Assert.Empty(logger.Entries);
    }
}
