using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hugo;
using NSubstitute;
using OmniRelay.Core;
using OmniRelay.Core.Clients;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using Xunit;
using static Hugo.Go;

namespace OmniRelay.Core.UnitTests.Clients;

public class ClientStreamClientTests
{
    public sealed class Req { public int V { get; init; } }
    public sealed class Res { public string? S { get; init; } }

    private sealed class TestClientStreamTransportCall : IClientStreamTransportCall
    {
        private readonly List<ReadOnlyMemory<byte>> _writes = new();
        private readonly TaskCompletionSource<Result<Response<ReadOnlyMemory<byte>>>> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TestClientStreamTransportCall(RequestMeta meta)
        {
            RequestMeta = meta;
            ResponseMeta = new ResponseMeta();
        }

        public RequestMeta RequestMeta { get; }
        public ResponseMeta ResponseMeta { get; set; }
        public Task<Result<Response<ReadOnlyMemory<byte>>>> Response => _tcs.Task;
        public IReadOnlyList<ReadOnlyMemory<byte>> Writes => _writes;

        public ValueTask WriteAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
        {
            _writes.Add(payload);
            return ValueTask.CompletedTask;
        }

        public ValueTask CompleteAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void CompleteWith(Result<Response<ReadOnlyMemory<byte>>> result)
        {
            if (result.IsSuccess)
            {
                ResponseMeta = result.Value.Meta;
            }
            _tcs.TrySetResult(result);
        }
    }

    [Fact]
    public async Task Start_Write_Complete_Response_Decode_Succeeds()
    {
        var outbound = Substitute.For<IClientStreamOutbound>();
        var codec = Substitute.For<ICodec<Req, Res>>();
        codec.Encoding.Returns("json");
        codec.EncodeRequest(Arg.Any<Req>(), Arg.Any<RequestMeta>()).Returns(ci => Ok(new byte[] { (byte)ci.Arg<Req>().V }));
        codec.DecodeResponse(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<ResponseMeta>()).Returns(ci => Ok(new Res { S = Convert.ToBase64String(ci.Arg<ReadOnlyMemory<byte>>().ToArray()) }));

        var meta = new RequestMeta();
        var transportCall = new TestClientStreamTransportCall(meta);
        outbound.CallAsync(Arg.Any<RequestMeta>(), Arg.Any<CancellationToken>()).Returns(ci => ValueTask.FromResult(Ok((IClientStreamTransportCall)transportCall)));

        var client = new ClientStreamClient<Req, Res>(outbound, codec, Array.Empty<IClientStreamOutboundMiddleware>());
        await using var session = await client.StartAsync(meta, TestContext.Current.CancellationToken);

        await session.WriteAsync(new Req { V = 10 }, TestContext.Current.CancellationToken);
        await session.WriteAsync(new Req { V = 20 }, TestContext.Current.CancellationToken);
        await session.CompleteAsync(TestContext.Current.CancellationToken);

        var responseBytes = new byte[] { 99 };
        var finalMeta = new ResponseMeta { Transport = "test" };
        transportCall.CompleteWith(Ok(Response<ReadOnlyMemory<byte>>.Create(responseBytes, finalMeta)));

    var response = await session.Response;
    Assert.Equal(Convert.ToBase64String(responseBytes), response.Body.S);
    }
}
