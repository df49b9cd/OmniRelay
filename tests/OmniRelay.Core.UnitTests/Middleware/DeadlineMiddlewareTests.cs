using System;
using System.Threading;
using System.Threading.Tasks;
using Hugo;
using OmniRelay.Core;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using OmniRelay.Errors;
using Xunit;
using static Hugo.Go;

namespace OmniRelay.Core.UnitTests.Middleware;

public class DeadlineMiddlewareTests
{
    private static IRequest<ReadOnlyMemory<byte>> MakeReq(RequestMeta meta) => new Request<ReadOnlyMemory<byte>>(meta, ReadOnlyMemory<byte>.Empty);

    [Fact]
    public async Task PastDeadline_FailsImmediately()
    {
        var mw = new DeadlineMiddleware();
        var meta = new RequestMeta(service: "svc", procedure: "proc", deadline: DateTimeOffset.UtcNow.AddSeconds(-1));
        UnaryOutboundDelegate next = (req, ct) => ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty)));

        var res = await mw.InvokeAsync(MakeReq(meta), TestContext.Current.CancellationToken, next);
        Assert.True(res.IsFailure);
        Assert.Equal(OmniRelayStatusCode.DeadlineExceeded, OmniRelayErrorAdapter.ToStatus(res.Error!));
    }

    [Fact]
    public async Task TtlBelowLeadTime_FailsImmediately()
    {
        var mw = new DeadlineMiddleware(new DeadlineOptions { MinimumLeadTime = TimeSpan.FromSeconds(5) });
        var meta = new RequestMeta(service: "svc", procedure: "proc", timeToLive: TimeSpan.FromSeconds(1));
        UnaryOutboundDelegate next = (req, ct) => ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty)));

        var res = await mw.InvokeAsync(MakeReq(meta), TestContext.Current.CancellationToken, next);
        Assert.True(res.IsFailure);
        Assert.Equal(OmniRelayStatusCode.DeadlineExceeded, OmniRelayErrorAdapter.ToStatus(res.Error!));
    }

    [Fact]
    public async Task FutureDeadline_LinksCancellationToken()
    {
        var mw = new DeadlineMiddleware();
        var meta = new RequestMeta(service: "svc", procedure: "proc", deadline: DateTimeOffset.UtcNow.AddMilliseconds(50));
        var called = false;
        UnaryOutboundDelegate next = async (req, ct) =>
        {
            called = true;
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty));
        };

        var res = await mw.InvokeAsync(MakeReq(meta), TestContext.Current.CancellationToken, next);
        Assert.True(called);
        Assert.True(res.IsFailure);
        Assert.Equal(OmniRelayStatusCode.DeadlineExceeded, OmniRelayErrorAdapter.ToStatus(res.Error!));
    }
}
