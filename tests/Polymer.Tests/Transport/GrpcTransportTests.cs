using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Polymer.Core;
using Polymer.Core.Transport;
using Polymer.Dispatcher;
using Polymer.Transport.Grpc;
using Polymer.Errors;
using Xunit;
using static Hugo.Go;

namespace Polymer.Tests.Transport;

public class GrpcTransportTests
{
    static GrpcTransportTests()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    [Fact(Timeout = 30_000)]
    public async Task ServerStreaming_OverGrpcTransport()
    {
        var port = TestPortAllocator.GetRandomPort();
        var address = new Uri($"http://127.0.0.1:{port}");

        var options = new DispatcherOptions("stream");
        var grpcInbound = new GrpcInbound([address.ToString()]);
        options.AddLifecycle("grpc-inbound", grpcInbound);

        var grpcOutbound = new GrpcOutbound(address, "stream");
        options.AddStreamOutbound("stream", null, grpcOutbound);

        var dispatcher = new Polymer.Dispatcher.Dispatcher(options);
        var codec = new JsonCodec<EchoRequest, EchoResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        await using var serverTasks = new ServerTaskTracker();

        dispatcher.Register(new StreamProcedureSpec(
            "stream",
            "stream::events",
            (request, callOptions, cancellationToken) =>
            {
                var decode = codec.DecodeRequest(request.Body, request.Meta);
                if (decode.IsFailure)
                {
                    return ValueTask.FromResult(Err<IStreamCall>(decode.Error!));
                }

                var streamCall = GrpcServerStreamCall.Create(request.Meta, new ResponseMeta(encoding: "application/json"));

                var backgroundTask = Task.Run(async () =>
                {
                    try
                    {
                        for (var i = 0; i < 3; i++)
                        {
                            var response = new EchoResponse { Message = $"event-{i}" };
                            var encode = codec.EncodeResponse(response, streamCall.ResponseMeta);
                            if (encode.IsFailure)
                            {
                                await streamCall.CompleteAsync(encode.Error!).ConfigureAwait(false);
                                return;
                            }

                            await streamCall.WriteAsync(encode.Value, cancellationToken).ConfigureAwait(false);
                            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await streamCall.CompleteAsync().ConfigureAwait(false);
                    }
                }, cancellationToken);
                serverTasks.Track(backgroundTask);

                return ValueTask.FromResult(Ok<IStreamCall>(streamCall));
            }));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);
        await WaitForGrpcReadyAsync(address, ct);

        try
        {
            var client = dispatcher.CreateStreamClient<EchoRequest, EchoResponse>("stream", codec);
            var requestMeta = new RequestMeta(
                service: "stream",
                procedure: "stream::events",
                encoding: "application/json",
                transport: "grpc");
            var request = new Request<EchoRequest>(requestMeta, new EchoRequest("seed"));

            var responses = new List<string>();
            await foreach (var response in client.CallAsync(request, new StreamCallOptions(StreamDirection.Server), ct))
            {
                responses.Add(response.Body.Message);
            }

            Assert.Equal(new[] { "event-0", "event-1", "event-2" }, responses);
        }
        finally
        {
            await dispatcher.StopAsync(ct);
        }
    }

    [Fact(Timeout = 30_000)]
    public async Task ClientStreaming_OverGrpcTransport()
    {
        var port = TestPortAllocator.GetRandomPort();
        var address = new Uri($"http://127.0.0.1:{port}");

        var options = new DispatcherOptions("stream");
        var grpcInbound = new GrpcInbound([address.ToString()]);
        options.AddLifecycle("grpc-inbound", grpcInbound);

        var grpcOutbound = new GrpcOutbound(address, "stream");
        options.AddClientStreamOutbound("stream", null, grpcOutbound);

        var dispatcher = new Polymer.Dispatcher.Dispatcher(options);
        var codec = new JsonCodec<AggregateChunk, AggregateResponse>(encoding: "application/json");

        dispatcher.Register(new ClientStreamProcedureSpec(
            "stream",
            "stream::aggregate",
            async (context, cancellationToken) =>
            {
                var totalBytes = 0;
                var reader = context.Requests;

                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var payload))
                    {
                        var decodeResult = codec.DecodeRequest(payload, context.Meta);
                        if (decodeResult.IsFailure)
                        {
                            return Err<Response<ReadOnlyMemory<byte>>>(decodeResult.Error!);
                        }

                        totalBytes += decodeResult.Value.Amount;
                    }
                }

                var aggregateResponse = new AggregateResponse(totalBytes);
                var responseMeta = new ResponseMeta(encoding: codec.Encoding);
                var encodeResponse = codec.EncodeResponse(aggregateResponse, responseMeta);
                if (encodeResponse.IsFailure)
                {
                    return Err<Response<ReadOnlyMemory<byte>>>(encodeResponse.Error!);
                }

                var response = Response<ReadOnlyMemory<byte>>.Create(encodeResponse.Value, responseMeta);
                return Ok(response);
            }));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);
        await WaitForGrpcReadyAsync(address, ct);

        try
        {
            var client = dispatcher.CreateClientStreamClient<AggregateChunk, AggregateResponse>("stream", codec);

            var requestMeta = new RequestMeta(
                service: "stream",
                procedure: "stream::aggregate",
                encoding: codec.Encoding,
                transport: "grpc");

            await using var stream = await client.StartAsync(requestMeta, ct);

            await stream.WriteAsync(new AggregateChunk(Amount: 2), ct);
            await stream.WriteAsync(new AggregateChunk(Amount: 5), ct);
            await stream.CompleteAsync(ct);

            var response = await stream.Response;

            Assert.Equal(7, response.Body.TotalAmount);
            Assert.Equal(codec.Encoding, stream.ResponseMeta.Encoding);
        }
        finally
        {
            await dispatcher.StopAsync(ct);
        }
    }

    [Fact(Timeout = 30_000)]
    public async Task ClientStreaming_CancellationFromClient()
    {
        var port = TestPortAllocator.GetRandomPort();
        var address = new Uri($"http://127.0.0.1:{port}");

        var options = new DispatcherOptions("stream");
        var grpcInbound = new GrpcInbound([address.ToString()]);
        options.AddLifecycle("grpc-inbound", grpcInbound);

        var grpcOutbound = new GrpcOutbound(address, "stream");
        options.AddClientStreamOutbound("stream", null, grpcOutbound);

        var dispatcher = new Polymer.Dispatcher.Dispatcher(options);
        var codec = new JsonCodec<AggregateChunk, AggregateResponse>(encoding: "application/json");
        dispatcher.Register(new ClientStreamProcedureSpec(
            "stream",
            "stream::aggregate",
            async (context, cancellationToken) =>
            {
                await foreach (var _ in context.Requests.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Simply drain until cancellation.
                }

                return Err<Response<ReadOnlyMemory<byte>>>(PolymerErrorAdapter.FromStatus(
                    PolymerStatusCode.Cancelled,
                    "cancelled"));
            }));

        var cts = new CancellationTokenSource();
        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);
        await WaitForGrpcReadyAsync(address, ct);

        try
        {
            var client = dispatcher.CreateClientStreamClient<AggregateChunk, AggregateResponse>("stream", codec);
            var requestMeta = new RequestMeta(service: "stream", procedure: "stream::aggregate", encoding: codec.Encoding, transport: "grpc");

            await using var stream = await client.StartAsync(requestMeta, ct);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await stream.WriteAsync(new AggregateChunk(Amount: 1), cts.Token);
            });
        }
        finally
        {
            await dispatcher.StopAsync(ct);
        }
    }

    [Fact(Timeout = 30_000)]
    public async Task ClientStreaming_DeadlineExceededMapsStatus()
    {
        var port = TestPortAllocator.GetRandomPort();
        var address = new Uri($"http://127.0.0.1:{port}");

        var options = new DispatcherOptions("stream");
        var grpcInbound = new GrpcInbound([address.ToString()]);
        options.AddLifecycle("grpc-inbound", grpcInbound);

        var grpcOutbound = new GrpcOutbound(address, "stream");
        options.AddClientStreamOutbound("stream", null, grpcOutbound);

        var dispatcher = new Polymer.Dispatcher.Dispatcher(options);
        var codec = new JsonCodec<AggregateChunk, AggregateResponse>(encoding: "application/json");

        dispatcher.Register(new ClientStreamProcedureSpec(
            "stream",
            "stream::deadline",
            async (context, cancellationToken) =>
            {
                Assert.True(context.Meta.Deadline.HasValue);
                await foreach (var _ in context.Requests.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                }

                return Err<Response<ReadOnlyMemory<byte>>>(PolymerErrorAdapter.FromStatus(
                    PolymerStatusCode.DeadlineExceeded,
                    "deadline exceeded"));
            }));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);
        await WaitForGrpcReadyAsync(address, ct);

        try
        {
            var client = dispatcher.CreateClientStreamClient<AggregateChunk, AggregateResponse>("stream", codec);
            var requestMeta = new RequestMeta(
                service: "stream",
                procedure: "stream::deadline",
                encoding: codec.Encoding,
                transport: "grpc",
                deadline: DateTimeOffset.UtcNow.AddMilliseconds(200));

            await using var stream = await client.StartAsync(requestMeta, ct);
            await stream.CompleteAsync(ct);

            var exception = await Assert.ThrowsAsync<PolymerException>(async () => await stream.Response);
            Assert.Equal(PolymerStatusCode.DeadlineExceeded, exception.StatusCode);
        }
        finally
        {
            await dispatcher.StopAsync(ct);
        }
    }

    [Fact(Timeout = 30_000)]
    public async Task ClientStreaming_LargePayloadChunks()
    {
        var port = TestPortAllocator.GetRandomPort();
        var address = new Uri($"http://127.0.0.1:{port}");

        var options = new DispatcherOptions("stream");
        var grpcInbound = new GrpcInbound([address.ToString()]);
        options.AddLifecycle("grpc-inbound", grpcInbound);

        var grpcOutbound = new GrpcOutbound(address, "stream");
        options.AddClientStreamOutbound("stream", null, grpcOutbound);

        var dispatcher = new Polymer.Dispatcher.Dispatcher(options);
        var codec = new JsonCodec<AggregateChunk, AggregateResponse>(encoding: "application/json");

        dispatcher.Register(new ClientStreamProcedureSpec(
            "stream",
            "stream::huge",
            async (context, cancellationToken) =>
            {
                var total = 0;
                await foreach (var payload in context.Requests.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    var decode = codec.DecodeRequest(payload, context.Meta);
                    if (decode.IsFailure)
                    {
                        return Err<Response<ReadOnlyMemory<byte>>>(decode.Error!);
                    }

                    total += decode.Value.Amount;
                }

                var response = new AggregateResponse(total);
                var responseMeta = new ResponseMeta(encoding: codec.Encoding);
                var encode = codec.EncodeResponse(response, responseMeta);
                if (encode.IsFailure)
                {
                    return Err<Response<ReadOnlyMemory<byte>>>(encode.Error!);
                }

                return Ok(Response<ReadOnlyMemory<byte>>.Create(encode.Value, responseMeta));
            }));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);
        await WaitForGrpcReadyAsync(address, ct);

        try
        {
            var client = dispatcher.CreateClientStreamClient<AggregateChunk, AggregateResponse>("stream", codec);
            var requestMeta = new RequestMeta(service: "stream", procedure: "stream::huge", encoding: codec.Encoding, transport: "grpc");

            await using var stream = await client.StartAsync(requestMeta, ct);

            const int chunkCount = 1_000;
            for (var i = 0; i < chunkCount; i++)
            {
                await stream.WriteAsync(new AggregateChunk(1), ct);
            }

            await stream.CompleteAsync(ct);

            var response = await stream.Response;
            Assert.Equal(chunkCount, response.Body.TotalAmount);
        }
        finally
        {
            await dispatcher.StopAsync(ct);
        }
    }

    [Fact(Timeout = 30_000)]
    public async Task UnaryRoundtrip_OverGrpcTransport()
    {
        var port = TestPortAllocator.GetRandomPort();
        var address = new Uri($"http://127.0.0.1:{port}");

        var options = new DispatcherOptions("echo");
        var grpcInbound = new GrpcInbound([address.ToString()]);
        options.AddLifecycle("grpc-inbound", grpcInbound);

        var grpcOutbound = new GrpcOutbound(address, "echo");
        options.AddUnaryOutbound("echo", null, grpcOutbound);

        var dispatcher = new Polymer.Dispatcher.Dispatcher(options);
        var codec = new JsonCodec<EchoRequest, EchoResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        dispatcher.Register(new UnaryProcedureSpec(
            "echo",
            "ping",
            (request, cancellationToken) =>
            {
                var decodeResult = codec.DecodeRequest(request.Body, request.Meta);
                if (decodeResult.IsFailure)
                {
                    return ValueTask.FromResult(Err<Response<ReadOnlyMemory<byte>>>(decodeResult.Error!));
                }

                var responsePayload = new EchoResponse { Message = decodeResult.Value.Message + "-grpc" };
                var encodeResult = codec.EncodeResponse(responsePayload, new ResponseMeta(encoding: "application/json"));
                if (encodeResult.IsFailure)
                {
                    return ValueTask.FromResult(Err<Response<ReadOnlyMemory<byte>>>(encodeResult.Error!));
                }

                var response = Response<ReadOnlyMemory<byte>>.Create(encodeResult.Value, new ResponseMeta(encoding: "application/json"));
                return ValueTask.FromResult(Ok(response));
            }));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);

        await WaitForGrpcReadyAsync(address, ct);

        try
        {
            var client = dispatcher.CreateUnaryClient<EchoRequest, EchoResponse>("echo", codec);
            var requestMeta = new RequestMeta(
                service: "echo",
                procedure: "ping",
                encoding: "application/json",
                transport: "grpc");
            var request = new Request<EchoRequest>(requestMeta, new EchoRequest("hello"));

            var result = await client.CallAsync(request, ct);

            Assert.True(result.IsSuccess, result.Error?.Message);
            Assert.Equal("hello-grpc", result.Value.Body.Message);
        }
        finally
        {
            await dispatcher.StopAsync(ct);
        }
    }

    [Fact(Timeout = 30_000)]
    public async Task OnewayRoundtrip_OverGrpcTransport()
    {
        var port = TestPortAllocator.GetRandomPort();
        var address = new Uri($"http://127.0.0.1:{port}");

        var options = new DispatcherOptions("audit");
        var grpcInbound = new GrpcInbound([address.ToString()]);
        options.AddLifecycle("grpc-inbound", grpcInbound);

        var grpcOutbound = new GrpcOutbound(address, "audit");
        options.AddOnewayOutbound("audit", null, grpcOutbound);

        var dispatcher = new Polymer.Dispatcher.Dispatcher(options);
        var codec = new JsonCodec<EchoRequest, object>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        dispatcher.Register(new OnewayProcedureSpec(
            "audit",
            "audit::record",
            (request, cancellationToken) =>
            {
                var decodeResult = codec.DecodeRequest(request.Body, request.Meta);
                if (decodeResult.IsFailure)
                {
                    return ValueTask.FromResult(Err<OnewayAck>(decodeResult.Error!));
                }

                received.TrySetResult(decodeResult.Value.Message);
                return ValueTask.FromResult(Ok(OnewayAck.Ack()));
            }));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);
        await WaitForGrpcReadyAsync(address, ct);

        try
        {
            var client = dispatcher.CreateOnewayClient<EchoRequest>("audit", codec);
            var requestMeta = new RequestMeta(
                service: "audit",
                procedure: "audit::record",
                encoding: "application/json",
                transport: "grpc");
            var request = new Request<EchoRequest>(requestMeta, new EchoRequest("ping"));

            var ackResult = await client.CallAsync(request, ct);

            Assert.True(ackResult.IsSuccess, ackResult.Error?.Message);
            Assert.Equal("ping", await received.Task.WaitAsync(TimeSpan.FromSeconds(2), ct));
        }
        finally
        {
            await dispatcher.StopAsync(ct);
        }
    }

    private static async Task WaitForGrpcReadyAsync(Uri address, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(address);

        const int maxAttempts = 100;
        const int connectTimeoutMilliseconds = 200;
        const int settleDelayMilliseconds = 50;
        const int retryDelayMilliseconds = 20;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(address.Host, address.Port)
                            .WaitAsync(TimeSpan.FromMilliseconds(connectTimeoutMilliseconds), cancellationToken)
                            .ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromMilliseconds(settleDelayMilliseconds), cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SocketException)
            {
                // Listener not ready yet; retry.
            }
            catch (TimeoutException)
            {
                // Connection attempt timed out; retry.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(retryDelayMilliseconds), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("The gRPC inbound failed to bind within the allotted time.");
    }

    private sealed class ServerTaskTracker : IAsyncDisposable
    {
        private readonly List<Task> _tasks = new();

        public void Track(Task task)
        {
            if (task is null)
            {
                return;
            }

            lock (_tasks)
            {
                _tasks.Add(task);
            }
        }

        public async ValueTask DisposeAsync()
        {
            Task[] toAwait;
            lock (_tasks)
            {
                if (_tasks.Count == 0)
                {
                    return;
                }

                toAwait = _tasks.ToArray();
                _tasks.Clear();
            }

            try
            {
                await Task.WhenAll(toAwait).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation tokens propagate during shutdown.
            }
        }
    }

    private sealed record EchoRequest(string Message);

    private sealed record EchoResponse
    {
        public string Message { get; init; } = string.Empty;
    }

    private sealed record AggregateChunk(int Amount);

    private sealed record AggregateResponse(int TotalAmount);
}
