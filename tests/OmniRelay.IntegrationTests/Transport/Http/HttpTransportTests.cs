using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using OmniRelay.Core;
using OmniRelay.Core.Transport;
using OmniRelay.Dispatcher;
using OmniRelay.Errors;
using OmniRelay.IntegrationTests.Support;
using OmniRelay.TestSupport.Assertions;
using OmniRelay.Transport.Http;
using Xunit;
using static Hugo.Go;
using static OmniRelay.IntegrationTests.Support.TransportTestHelper;

namespace OmniRelay.IntegrationTests.Transport;

public sealed class HttpTransportTests(ITestOutputHelper output) : TransportIntegrationTest(output)
{
    [Fact(Timeout = 30000)]
    public async ValueTask UnaryRoundtrip_EncodesAndDecodesPayload()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}/");

        var options = new DispatcherOptions("echo");
        var httpInbound = new HttpInbound([baseAddress.ToString()]);
        options.AddLifecycle("http-inbound", httpInbound);

        var httpClient = new HttpClient { BaseAddress = baseAddress };
        var httpOutbound = HttpOutbound.Create(httpClient, baseAddress, disposeClient: true).ValueOrChecked();
        options.AddUnaryOutbound("echo", null, httpOutbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);

        var codec = new JsonCodec<EchoRequest, EchoResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        dispatcher.Register(new UnaryProcedureSpec(
            "echo",
            "ping",
            async (request, cancellationToken) =>
            {
                var decodeResult = codec.DecodeRequest(request.Body, request.Meta);
                if (decodeResult.IsFailure)
                {
                    return Err<Response<ReadOnlyMemory<byte>>>(decodeResult.Error!);
                }

                var responsePayload = new EchoResponse { Message = decodeResult.Value.Message.ToUpperInvariant() };
                var encodeResult = codec.EncodeResponse(responsePayload, new ResponseMeta(encoding: "application/json"));
                if (encodeResult.IsFailure)
                {
                    return Err<Response<ReadOnlyMemory<byte>>>(encodeResult.Error!);
                }

                var response = Response<ReadOnlyMemory<byte>>.Create(encodeResult.Value, new ResponseMeta(encoding: "application/json"));
                return Ok(response);
            }));

        var ct = TestContext.Current.CancellationToken;
        await using var host = await StartDispatcherAsync(nameof(UnaryRoundtrip_EncodesAndDecodesPayload), dispatcher, ct);
        await WaitForHttpEndpointReadyAsync(baseAddress, ct);

        var clientResult = host.Dispatcher.CreateUnaryClient<EchoRequest, EchoResponse>("echo", codec);
        clientResult.IsSuccess.ShouldBeTrue(clientResult.Error?.Message);
        var client = clientResult.Value;

        var requestMeta = new RequestMeta(
            service: "echo",
            procedure: "ping",
            encoding: "application/json",
            transport: "http");

        var request = new Request<EchoRequest>(requestMeta, new EchoRequest("hello"));

        var result = await client.CallAsync(request, ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value.Body.Message.Should().Be("HELLO");
    }

    private sealed record EchoRequest(string Message)
    {
        public string Message { get; init; } = Message;
    }

    private sealed record EchoResponse
    {
        public string Message { get; init; } = string.Empty;
    }

    private sealed record ChatMessage(string Message)
    {
        public string Message { get; init; } = Message;
    }

    [Fact(Timeout = 30000)]
    public async ValueTask OnewayRoundtrip_SucceedsWithAck()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}/");

        var options = new DispatcherOptions("echo");
        var httpInbound = new HttpInbound([baseAddress.ToString()]);
        options.AddLifecycle("http-inbound", httpInbound);

        var httpClient = new HttpClient { BaseAddress = baseAddress };
        var httpOutbound = HttpOutbound.Create(httpClient, baseAddress, disposeClient: true).ValueOrChecked();
        options.AddOnewayOutbound("echo", null, httpOutbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var codec = new JsonCodec<EchoRequest, object>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        dispatcher.Register(new OnewayProcedureSpec(
            "echo",
            "notify",
            (request, cancellationToken) =>
            {
                var decodeResult = codec.DecodeRequest(request.Body, request.Meta);
                if (decodeResult.IsFailure)
                {
                    return ValueTask.FromResult(Err<OnewayAck>(decodeResult.Error!));
                }

                received.TrySetResult(decodeResult.Value.Message);
                return ValueTask.FromResult(Ok(OnewayAck.Ack(new ResponseMeta(encoding: "application/json"))));
            }));

        var ct = TestContext.Current.CancellationToken;
        await using var host = await StartDispatcherAsync(nameof(OnewayRoundtrip_SucceedsWithAck), dispatcher, ct);
        await WaitForHttpEndpointReadyAsync(baseAddress, ct);

        var onewayResult = host.Dispatcher.CreateOnewayClient<EchoRequest>("echo", codec);
        onewayResult.IsSuccess.ShouldBeTrue(onewayResult.Error?.Message);
        var client = onewayResult.Value;
        var requestMeta = new RequestMeta(
            service: "echo",
            procedure: "notify",
            encoding: "application/json",
            transport: "http");
        var request = new Request<EchoRequest>(requestMeta, new EchoRequest("ping"));

        var ackResult = await client.CallAsync(request, ct);

        ackResult.IsSuccess.Should().BeTrue(ackResult.Error?.Message);
        (await received.Task.WaitAsync(TimeSpan.FromSeconds(2), ct)).Should().Be("ping");
    }

    [Fact(Timeout = 30000)]
    public async ValueTask ServerStreaming_EmitsEventStream()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}/");

        var options = new DispatcherOptions("stream");
        var httpInbound = new HttpInbound([baseAddress.ToString()]);
        options.AddLifecycle("http-inbound", httpInbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);

        dispatcher.Register(new StreamProcedureSpec(
            "stream",
            "stream::events",
            (request, callOptions, cancellationToken) =>
            {
                var streamCall = HttpStreamCall.CreateServerStream(
                    request.Meta,
                    new ResponseMeta(encoding: "text/plain"));

                _ = Task.Run(async () =>
                {
                    try
                    {
                        for (var index = 0; index < 3; index++)
                        {
                            var payload = Encoding.UTF8.GetBytes($"event-{index}");
                            await streamCall.WriteAsync(payload, cancellationToken);
                            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
                        }
                    }
                    finally
                    {
                        await streamCall.CompleteAsync();
                    }
                }, cancellationToken);

                return ValueTask.FromResult(Ok<IStreamCall>(streamCall));
            }));

        var ct = TestContext.Current.CancellationToken;
        await using var host = await StartDispatcherAsync(nameof(ServerStreaming_EmitsEventStream), dispatcher, ct);
        await WaitForHttpEndpointReadyAsync(baseAddress, ct);

        using var httpClient = new HttpClient { BaseAddress = baseAddress };
        httpClient.DefaultRequestHeaders.Add(HttpTransportHeaders.Procedure, "stream::events");
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/event-stream");

        using var response = await httpClient.GetAsync("/", HttpCompletionOption.ResponseHeadersRead, ct);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
        var bufferingHeaderFound = response.Headers.TryGetValues("X-Accel-Buffering", out var xab) || response.Content.Headers.TryGetValues("X-Accel-Buffering", out xab);
        bufferingHeaderFound.Should().BeTrue();
        xab.Should().Contain("no");
        using var responseStream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(responseStream, Encoding.UTF8);

        var events = new List<string>();

        while (events.Count < 3)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                break;
            }

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                events.Add(line.Substring("data:".Length).Trim());
            }
        }

        events.Should().Equal("event-0", "event-1", "event-2");
    }

    [Fact(Timeout = 30000)]
    public async ValueTask ServerStreaming_BinaryPayloadsAreBase64Encoded()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}/");

        var options = new DispatcherOptions("stream-b64");
        var httpInbound = new HttpInbound([baseAddress.ToString()]);
        options.AddLifecycle("http-inbound", httpInbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);

        dispatcher.Register(new StreamProcedureSpec(
            "stream-b64",
            "stream::binary",
            (request, callOptions, cancellationToken) =>
            {
                _ = callOptions;
                var streamCall = HttpStreamCall.CreateServerStream(
                    request.Meta,
                    new ResponseMeta(encoding: "application/octet-stream"));

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var payload = new byte[] { 0x00, 0x01, 0x02 };
                        await streamCall.WriteAsync(payload, cancellationToken);
                    }
                    finally
                    {
                        await streamCall.CompleteAsync();
                    }
                }, cancellationToken);

                return ValueTask.FromResult(Ok<IStreamCall>(streamCall));
            }));

        var ct = TestContext.Current.CancellationToken;
        await using var host = await StartDispatcherAsync(nameof(ServerStreaming_BinaryPayloadsAreBase64Encoded), dispatcher, ct);
        await WaitForHttpEndpointReadyAsync(baseAddress, ct);

        using var httpClient = new HttpClient { BaseAddress = baseAddress };
        httpClient.DefaultRequestHeaders.Add(HttpTransportHeaders.Procedure, "stream::binary");
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/event-stream");

        using var response = await httpClient.GetAsync("/", HttpCompletionOption.ResponseHeadersRead, ct);
        response.IsSuccessStatusCode.Should().BeTrue();

        using var responseStream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(responseStream, Encoding.UTF8);

        var dataLine = await reader.ReadLineAsync(ct);
        var encodingLine = await reader.ReadLineAsync(ct);
        var blankLine = await reader.ReadLineAsync(ct);

        dataLine.Should().Be("data: AAEC");
        encodingLine.Should().Be("encoding: base64");
        blankLine.Should().Be(string.Empty);
    }

    [Fact(Timeout = 30000)]
    public async ValueTask ServerStreaming_PayloadAboveLimit_FaultsStream()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}/");

        var options = new DispatcherOptions("stream-limit");
        var runtime = new HttpServerRuntimeOptions { ServerStreamMaxMessageBytes = 8 };
        HttpStreamCall? streamCall = null;
        var httpInbound = new HttpInbound([baseAddress.ToString()], serverRuntimeOptions: runtime);
        options.AddLifecycle("http-inbound", httpInbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);

        dispatcher.Register(new StreamProcedureSpec(
            "stream-limit",
            "stream::oversized",
            (request, callOptions, cancellationToken) =>
            {
                _ = callOptions;
                var call = HttpStreamCall.CreateServerStream(
                    request.Meta,
                    new ResponseMeta(encoding: "text/plain"));
                streamCall = call;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var payload = "this-payload-is-way-too-long"u8.ToArray();
                        await call.WriteAsync(payload, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore cancellation when the response aborts
                    }
                }, cancellationToken);

                return ValueTask.FromResult(Ok<IStreamCall>(call));
            }));

        var ct = TestContext.Current.CancellationToken;
        await using var host = await StartDispatcherAsync(nameof(ServerStreaming_PayloadAboveLimit_FaultsStream), dispatcher, ct);
        await WaitForHttpEndpointReadyAsync(baseAddress, ct);

        using var httpClient = new HttpClient { BaseAddress = baseAddress };
        httpClient.DefaultRequestHeaders.Add(HttpTransportHeaders.Procedure, "stream::oversized");
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/event-stream");

        HttpResponseMessage? response = null;

        try
        {
            response = await httpClient.GetAsync("/", HttpCompletionOption.ResponseHeadersRead, ct);
            response.IsSuccessStatusCode.Should().BeTrue();
        }
        catch (HttpRequestException exception) when (IsConnectionReset(exception))
        {
            // Some platforms surface the aborted connection as a connection reset.
        }
        finally
        {
            response?.Dispose();
        }

        await WaitForCompletionAsync(ct);

        streamCall.Should().NotBeNull();
        streamCall!.Context.CompletionStatus.Should().Be(StreamCompletionStatus.Faulted);
        var completionError = streamCall.Context.CompletionError;
        completionError.Should().NotBeNull();
        OmniRelayErrorAdapter.ToStatus(completionError!).Should().Be(OmniRelayStatusCode.ResourceExhausted);

        async Task WaitForCompletionAsync(CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (streamCall is not null && streamCall.Context.CompletionStatus != StreamCompletionStatus.None)
                {
                    return;
                }

                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException("Server stream did not complete.");
                }

                await Task.Delay(50, cancellationToken);
            }
        }

        static bool IsConnectionReset(HttpRequestException exception)
        {
            if (exception.InnerException is IOException ioException &&
                ioException.InnerException is SocketException socketException &&
                socketException.SocketErrorCode == SocketError.ConnectionReset)
            {
                return true;
            }

            return false;
        }
    }

    [Fact(Timeout = 30000)]
    public async ValueTask DuplexStreaming_OverHttpWebSocket()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}/");

        var options = new DispatcherOptions("chat");
        var httpInbound = new HttpInbound([baseAddress.ToString()]);
        options.AddLifecycle("http-inbound", httpInbound);

        var httpDuplexOutbound = new HttpDuplexOutbound(baseAddress);
        options.AddDuplexOutbound("chat", null, httpDuplexOutbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);
        var codec = new JsonCodec<ChatMessage, ChatMessage>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, encoding: "application/json");

        dispatcher.Register(new DuplexProcedureSpec(
            "chat",
            "chat::echo",
            (request, cancellationToken) =>
            {
                var call = DuplexStreamCall.Create(request.Meta, new ResponseMeta(encoding: "application/json"));
                call.SetResponseMeta(new ResponseMeta(encoding: "application/json", transport: "http"));

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var payload in call.RequestReader.ReadAllAsync(cancellationToken))
                        {
                            var decode = codec.DecodeRequest(payload, request.Meta);
                            if (decode.IsFailure)
                            {
                                await call.CompleteResponsesAsync(decode.Error!, cancellationToken);
                                return;
                            }

                            var message = decode.Value;
                            var responsePayload = codec.EncodeResponse(message, call.ResponseMeta);
                            if (responsePayload.IsFailure)
                            {
                                await call.CompleteResponsesAsync(responsePayload.Error!, cancellationToken);
                                return;
                            }

                            await call.ResponseWriter.WriteAsync(responsePayload.Value, cancellationToken);
                        }

                        await call.CompleteResponsesAsync(cancellationToken: cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        await call.CompleteResponsesAsync(OmniRelayErrorAdapter.FromStatus(
                            OmniRelayStatusCode.Cancelled,
                            "cancelled",
                            transport: "http"), CancellationToken.None);
                    }
                }, cancellationToken);

                return ValueTask.FromResult(Ok((IDuplexStreamCall)call));
            }));

        var ct = TestContext.Current.CancellationToken;
        await using var host = await StartDispatcherAsync(nameof(DuplexStreaming_OverHttpWebSocket), dispatcher, ct);
        await WaitForHttpEndpointReadyAsync(baseAddress, ct);

        var duplexResult = host.Dispatcher.CreateDuplexStreamClient<ChatMessage, ChatMessage>("chat", codec);
        duplexResult.IsSuccess.ShouldBeTrue(duplexResult.Error?.Message);
        var client = duplexResult.Value;
        var requestMeta = new RequestMeta(
            service: "chat",
            procedure: "chat::echo",
            encoding: "application/json",
            transport: "http");

        var sessionResult = await client.StartAsync(requestMeta, ct);
        await using var session = sessionResult.ValueOrChecked();

        (await session.WriteAsync(new ChatMessage("hello"), ct)).ValueOrChecked();
        (await session.WriteAsync(new ChatMessage("world"), ct)).ValueOrChecked();
        await session.CompleteRequestsAsync(cancellationToken: ct);

        var messages = new List<string>();
        await foreach (var response in session.ReadResponsesAsync(ct))
        {
            messages.Add(response.ValueOrChecked().Body.Message);
        }

        messages.Should().Equal("hello", "world");
    }

    [Fact(Timeout = 30000)]
    public async ValueTask DuplexStreaming_ServerCancels_PropagatesToClient()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}/");

        var options = new DispatcherOptions("chat");
        var httpInbound = new HttpInbound([baseAddress.ToString()]);
        options.AddLifecycle("http-inbound", httpInbound);

        var httpDuplexOutbound = new HttpDuplexOutbound(baseAddress);
        options.AddDuplexOutbound("chat", null, httpDuplexOutbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);
        var codec = new JsonCodec<ChatMessage, ChatMessage>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, encoding: "application/json");

        dispatcher.Register(new DuplexProcedureSpec(
            "chat",
            "chat::echo",
            (request, cancellationToken) =>
            {
                var call = DuplexStreamCall.Create(request.Meta, new ResponseMeta(encoding: "application/json"));

                _ = Task.Run(async () =>
                {
                    // Avoid coupling to request cancellation to make test deterministic
                    Thread.Sleep(TimeSpan.FromMilliseconds(20));
                    await call.CompleteResponsesAsync(OmniRelayErrorAdapter.FromStatus(
                        OmniRelayStatusCode.Cancelled,
                        "cancelled",
                        transport: "http"), CancellationToken.None);
                }, CancellationToken.None);

                return ValueTask.FromResult(Ok((IDuplexStreamCall)call));
            }));

        var ct = TestContext.Current.CancellationToken;
        await using var host = await StartDispatcherAsync(nameof(DuplexStreaming_ServerCancels_PropagatesToClient), dispatcher, ct);
        await WaitForHttpEndpointReadyAsync(baseAddress, ct);

        var duplexResult = host.Dispatcher.CreateDuplexStreamClient<ChatMessage, ChatMessage>("chat", codec);
        duplexResult.IsSuccess.ShouldBeTrue(duplexResult.Error?.Message);
        var client = duplexResult.Value;
        var requestMeta = new RequestMeta(
            service: "chat",
            procedure: "chat::echo",
            transport: "http");

        var sessionResult = await client.StartAsync(requestMeta, ct);
        await using var session = sessionResult.ValueOrChecked();

        await using var enumerator = session.ReadResponsesAsync(ct).GetAsyncEnumerator(ct);
        (await enumerator.MoveNextAsync()).Should().BeTrue();
        enumerator.Current.IsFailure.Should().BeTrue();
        OmniRelayErrorAdapter.ToStatus(enumerator.Current.Error!)
            .Should().BeOneOf(OmniRelayStatusCode.Cancelled, OmniRelayStatusCode.Unknown);
    }
}
