using System;
using System.Threading;
using System.Threading.Tasks;
using Hugo;
using Hugo.Policies;
using OmniRelay.Core;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using Xunit;
using static Hugo.Go;

namespace OmniRelay.Core.UnitTests.Middleware;

public class RetryMiddlewareTests
{
    private static IRequest<ReadOnlyMemory<byte>> MakeReq(RequestMeta meta) => new Request<ReadOnlyMemory<byte>>(meta, ReadOnlyMemory<byte>.Empty);

    [Fact]
    public async Task ShouldRetryRequest_False_DoesNotRetry()
    {
        var options = new RetryOptions
        {
            ShouldRetryRequest = _ => false
        };
        var mw = new RetryMiddleware(options);

        var attempts = 0;
        UnaryOutboundDelegate next = (req, ct) =>
        {
            attempts++;
            return ValueTask.FromResult(Err<Response<ReadOnlyMemory<byte>>>(Error.Timeout()));
        };

        var res = await mw.InvokeAsync(MakeReq(new RequestMeta(service: "svc")), TestContext.Current.CancellationToken, next);
        Assert.True(res.IsFailure);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ShouldRetryError_False_DoesNotRetry()
    {
        var options = new RetryOptions
        {
            ShouldRetryError = _ => false
        };
        var mw = new RetryMiddleware(options);

        var attempts = 0;
        UnaryOutboundDelegate next = (req, ct) =>
        {
            attempts++;
            return ValueTask.FromResult(Err<Response<ReadOnlyMemory<byte>>>(Error.Timeout()));
        };

        var res = await mw.InvokeAsync(MakeReq(new RequestMeta(service: "svc")), TestContext.Current.CancellationToken, next);
        Assert.True(res.IsFailure);
        Assert.Equal(1, attempts);
    }
}
