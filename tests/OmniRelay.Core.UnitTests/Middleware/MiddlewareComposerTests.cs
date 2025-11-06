using System;
using System.Threading;
using System.Threading.Tasks;
using Hugo;
using NSubstitute;
using OmniRelay.Core;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using Xunit;
using static Hugo.Go;

namespace OmniRelay.Core.UnitTests.Middleware;

public class MiddlewareComposerTests
{
    [Fact]
    public async Task ComposeUnaryOutbound_ChainsInReverseOrder()
    {
        var order = new System.Collections.Generic.List<int>();
        var m1 = new TestUnaryMw(1, order);
        var m2 = new TestUnaryMw(2, order);
        var m3 = new TestUnaryMw(3, order);

        var terminalCalled = false;
        UnaryOutboundDelegate terminal = (req, ct) =>
        {
            terminalCalled = true;
            return ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty)));
        };

    var composed = MiddlewareComposer.ComposeUnaryOutbound(new IUnaryOutboundMiddleware[] { m1, m2, m3 }, terminal);
        var result = await composed(Request<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(terminalCalled);
    Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    private sealed class TestUnaryMw(int id, System.Collections.Generic.List<int> order) : IUnaryOutboundMiddleware
    {
        private readonly int _id = id;
        private readonly System.Collections.Generic.List<int> _order = order;
        public ValueTask<Result<Response<ReadOnlyMemory<byte>>>> InvokeAsync(IRequest<ReadOnlyMemory<byte>> request, CancellationToken cancellationToken, UnaryOutboundDelegate next)
        {
            _order.Add(_id);
            return next(request, cancellationToken);
        }
    }

    [Fact]
    public void Compose_ReturnsTerminal_WhenEmpty()
    {
        UnaryOutboundDelegate terminal = (req, ct) => ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty)));
        var composed = MiddlewareComposer.ComposeUnaryOutbound(Array.Empty<IUnaryOutboundMiddleware>(), terminal);
        Assert.Same(terminal, composed);
    }
}
