using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Hugo;
using OmniRelay.Core;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using OmniRelay.Errors;
using Xunit;
using static Hugo.Go;

namespace OmniRelay.Core.UnitTests.Middleware;

public class RateLimitingMiddlewareTests
{
    private static IRequest<ReadOnlyMemory<byte>> MakeReq(RequestMeta meta) => new Request<ReadOnlyMemory<byte>>(meta, ReadOnlyMemory<byte>.Empty);

    [Fact]
    public async Task Unary_WhenNoPermits_ReturnsResourceExhausted()
    {
    using var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = 0 });
    // Exhaust the single permit so middleware fails to acquire
    var preLease = await limiter.AcquireAsync(1, TestContext.Current.CancellationToken);
    Assert.True(preLease.IsAcquired);
        var mw = new RateLimitingMiddleware(new RateLimitingOptions { Limiter = limiter });
        var meta = new RequestMeta(service: "svc", procedure: "proc");

        UnaryOutboundDelegate next = (req, ct) => ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty)));
        var res = await mw.InvokeAsync(MakeReq(meta), TestContext.Current.CancellationToken, next);

    Assert.True(res.IsFailure);
    Assert.Equal(OmniRelayStatusCode.ResourceExhausted, OmniRelayErrorAdapter.ToStatus(res.Error!));
    preLease.Dispose();
    }

    [Fact]
    public async Task Stream_LeaseReleasedOnDispose()
    {
        using var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = 0 });
        var mw = new RateLimitingMiddleware(new RateLimitingOptions { Limiter = limiter });
        var meta = new RequestMeta(service: "svc", procedure: "proc");

        var leaseHeld = new TaskCompletionSource();
        StreamOutboundDelegate next = async (req, options, ct) =>
        {
            // Acquire a second time should fail until previous lease disposed
            var lease = await limiter.AcquireAsync(1, ct);
            if (lease.IsAcquired)
            {
                lease.Dispose();
                return Ok<IStreamCall>(ServerStreamCall.Create(meta));
            }
            // signal lease was held by middleware
            leaseHeld.TrySetResult();
            return Ok<IStreamCall>(ServerStreamCall.Create(meta));
        };

        var result = await mw.InvokeAsync(MakeReq(meta), new StreamCallOptions(StreamDirection.Server), TestContext.Current.CancellationToken, next);
        Assert.True(result.IsSuccess);
        await result.Value.DisposeAsync();

        // After dispose, acquiring should succeed quickly
        var lease2 = await limiter.AcquireAsync(1, TestContext.Current.CancellationToken);
    Assert.True(lease2.IsAcquired);
    lease2.Dispose();
    }
}
