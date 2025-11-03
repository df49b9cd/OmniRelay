using System.Text.Json;
using Polymer.Core;
using Polymer.Core.Transport;
using Polymer.Dispatcher;
using Polymer.Transport.Http;
using static Hugo.Go;

var port = 8080;
var durationSeconds = 0;

for (var index = 0; index < args.Length; index++)
{
    switch (args[index])
    {
        case "--port" when index + 1 < args.Length && int.TryParse(args[index + 1], out var parsedPort):
            port = parsedPort;
            index++;
            break;
        case "--duration" when index + 1 < args.Length && int.TryParse(args[index + 1], out var parsedDuration):
            durationSeconds = parsedDuration;
            index++;
            break;
    }
}

var address = new Uri($"http://127.0.0.1:{port}");
var dispatcherOptions = new DispatcherOptions("echo");
var httpInbound = new HttpInbound(new[] { address.ToString() });
dispatcherOptions.AddLifecycle("http-inbound", httpInbound);

var dispatcher = new Polymer.Dispatcher.Dispatcher(dispatcherOptions);
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
var codec = new JsonCodec<EchoRequest, EchoResponse>(jsonOptions);

dispatcher.Register(new UnaryProcedureSpec(
    "echo",
    "echo::ping",
    (request, cancellationToken) =>
    {
        var decode = codec.DecodeRequest(request.Body, request.Meta);
        if (decode.IsFailure)
        {
            return ValueTask.FromResult(Err<Response<ReadOnlyMemory<byte>>>(decode.Error!));
        }

        var responsePayload = new EchoResponse { Message = decode.Value.Message };
        var responseMeta = new ResponseMeta(encoding: "application/json");
        var encode = codec.EncodeResponse(responsePayload, responseMeta);
        if (encode.IsFailure)
        {
            return ValueTask.FromResult(Err<Response<ReadOnlyMemory<byte>>>(encode.Error!));
        }

        return ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(encode.Value, responseMeta)));
    }));

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

try
{
    await dispatcher.StartAsync(cts.Token).ConfigureAwait(false);
    Console.WriteLine($"Polymer echo server listening on {address}");

    if (durationSeconds > 0)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown via cancellation
        }
    }
    else
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token).ConfigureAwait(false);
    }
}
catch (OperationCanceledException)
{
    // shutting down
}
finally
{
    await dispatcher.StopAsync(CancellationToken.None).ConfigureAwait(false);
}

public sealed record EchoRequest
{
    public string Message { get; init; } = string.Empty;
}

public sealed record EchoResponse
{
    public string Message { get; init; } = string.Empty;
}
