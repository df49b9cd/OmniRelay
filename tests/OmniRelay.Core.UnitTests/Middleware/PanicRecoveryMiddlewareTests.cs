using System;
using System.Threading;
using System.Threading.Tasks;
using Hugo;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OmniRelay.Core;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using OmniRelay.Errors;
using Xunit;
using static Hugo.Go;

namespace OmniRelay.Core.UnitTests.Middleware;

public class PanicRecoveryMiddlewareTests
{
    [Fact]
    public async Task ConvertsExceptionToError_UnaryOutbound()
    {
        var logger = Substitute.For<ILogger<PanicRecoveryMiddleware>>();
        var mw = new PanicRecoveryMiddleware(logger);
        var meta = new RequestMeta(service: "svc", procedure: "proc", transport: "http");
        UnaryOutboundDelegate next = (req, ct) => throw new InvalidOperationException("boom");

        var result = await mw.InvokeAsync(new Request<ReadOnlyMemory<byte>>(meta, ReadOnlyMemory<byte>.Empty), TestContext.Current.CancellationToken, next);
    Assert.True(result.IsFailure);
    Assert.Equal(OmniRelayStatusCode.Internal, OmniRelayErrorAdapter.ToStatus(result.Error!));
    }
}
