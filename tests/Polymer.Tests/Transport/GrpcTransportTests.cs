using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Polymer.Core;
using Polymer.Core.Transport;
using Polymer.Dispatcher;
using Polymer.Transport.Grpc;
using Xunit;
using static Hugo.Go;

namespace Polymer.Tests.Transport;

public class GrpcTransportTests
{
    static GrpcTransportTests()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    [Fact]
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

                _ = Task.Run(async () =>
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

                return ValueTask.FromResult(Ok<IStreamCall>(streamCall));
            }));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);
        await Task.Delay(100, ct);

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

        await dispatcher.StopAsync(ct);
    }

    [Fact]
    public async Task ClientStreaming_OverGrpcTransport()
    {
        var port = TestPortAllocator.GetRandomPort();
        var address = new Uri($"http://127.0.0.1:{port}");

        var options = new DispatcherOptions("stream");
        var grpcInbound = new GrpcInbound([address.ToString()]);
        options.AddLifecycle("grpc-inbound", grpcInbound);

        var dispatcher = new Polymer.Dispatcher.Dispatcher(options);

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
                        totalBytes += payload.Length;
                    }
                }

                var responseMeta = new ResponseMeta(encoding: "application/octet-stream");
                var response = Response<ReadOnlyMemory<byte>>.Create(BitConverter.GetBytes(totalBytes), responseMeta);
                return Ok(response);
            }));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);
        await Task.Delay(100, ct);

        using var channel = GrpcChannel.ForAddress(address);
        var invoker = channel.CreateCallInvoker();
        var byteMarshaller = Marshallers.Create(
            (byte[] value) => value ?? Array.Empty<byte>(),
            payload => payload ?? Array.Empty<byte>());
        var method = new Method<byte[], byte[]>(
            MethodType.ClientStreaming,
            "stream",
            "stream::aggregate",
            byteMarshaller,
            byteMarshaller);

        var metadata = new Metadata
        {
            { "rpc-encoding", "application/octet-stream" }
        };

        var call = invoker.AsyncClientStreamingCall(method, null, new CallOptions(metadata, cancellationToken: ct));

        await call.RequestStream.WriteAsync(new byte[] { 0x01, 0x02 });
        await call.RequestStream.WriteAsync(new byte[] { 0x03 });
        await call.RequestStream.CompleteAsync();

        var responsePayload = await call.ResponseAsync;
        var count = BitConverter.ToInt32(responsePayload, 0);

        Assert.Equal(3, count);

        var responseHeaders = await call.ResponseHeadersAsync;
        Assert.Equal(
            "application/octet-stream",
            responseHeaders.FirstOrDefault(entry =>
                !entry.IsBinary && string.Equals(entry.Key, "polymer-encoding", StringComparison.OrdinalIgnoreCase))?.Value);

        await dispatcher.StopAsync(ct);
    }

    [Fact]
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

        // Allow Kestrel to finish binding before issuing the first call.
        await Task.Delay(100, ct);

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

        await dispatcher.StopAsync(ct);
    }

    [Fact]
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
        await Task.Delay(100, ct);

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

        await dispatcher.StopAsync(ct);
    }

    private sealed record EchoRequest(string Message);

    private sealed record EchoResponse
    {
        public string Message { get; init; } = string.Empty;
    }
}
