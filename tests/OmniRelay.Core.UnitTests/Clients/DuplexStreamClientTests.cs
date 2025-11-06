using System;
using System.Collections.Generic;
using System.Linq;
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

public class DuplexStreamClientTests
{
    public sealed class Req { public int A { get; init; } }
    public sealed class Res { public int B { get; init; } }

    [Fact]
    public async Task StartAsync_Writes_Encodes_And_Reads_Decoded_Responses()
    {
        var outbound = Substitute.For<IDuplexOutbound>();
        var codec = Substitute.For<ICodec<Req, Res>>();
        codec.Encoding.Returns("json");
        codec.EncodeRequest(Arg.Any<Req>(), Arg.Any<RequestMeta>()).Returns(ci => Ok(new byte[] { (byte)ci.Arg<Req>().A }));
        codec.DecodeResponse(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<ResponseMeta>()).Returns(ci => Ok(new Res { B = ci.Arg<ReadOnlyMemory<byte>>().Span[0] }));

        var meta = new RequestMeta();
        var duplex = DuplexStreamCall.Create(meta, new ResponseMeta { Transport = "test" });
        outbound.CallAsync(Arg.Any<IRequest<ReadOnlyMemory<byte>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ValueTask.FromResult(Ok((IDuplexStreamCall)duplex)));

        var client = new DuplexStreamClient<Req, Res>(outbound, codec, Array.Empty<IDuplexOutboundMiddleware>());
        var session = await client.StartAsync(meta, TestContext.Current.CancellationToken);

        await session.WriteAsync(new Req { A = 1 }, TestContext.Current.CancellationToken);
        await session.WriteAsync(new Req { A = 2 }, TestContext.Current.CancellationToken);
        await duplex.ResponseWriter.WriteAsync(new byte[] { 3 }, TestContext.Current.CancellationToken);
        await duplex.ResponseWriter.WriteAsync(new byte[] { 4 }, TestContext.Current.CancellationToken);
        await duplex.CompleteResponsesAsync(null, TestContext.Current.CancellationToken);

        var received = new List<Response<Res>>();
        await foreach (var r in session.ReadResponsesAsync(TestContext.Current.CancellationToken))
        {
            received.Add(r);
        }

        Assert.Equal(2, received.Count);
        Assert.Equal(3, received[0].Body.B);
        Assert.Equal(4, received[1].Body.B);
    }
}
