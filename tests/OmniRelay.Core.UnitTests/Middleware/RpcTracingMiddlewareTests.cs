using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hugo;
using OmniRelay.Core;
using OmniRelay.Core.Diagnostics;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using Xunit;
using static Hugo.Go;

namespace OmniRelay.Core.UnitTests.Middleware;

public class RpcTracingMiddlewareTests
{
    private sealed class TestRuntime : IDiagnosticsRuntime
    {
        public Microsoft.Extensions.Logging.LogLevel? MinimumLogLevel { get; private set; }
        public double? TraceSamplingProbability { get; private set; }
        public void SetMinimumLogLevel(Microsoft.Extensions.Logging.LogLevel? level) => MinimumLogLevel = level;
        public void SetTraceSamplingProbability(double? probability) => TraceSamplingProbability = probability;
    }

    [Fact]
    public async Task OutboundUnary_InjectsTraceparent()
    {
        using var source = new ActivitySource("test.tracing");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test.tracing",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        var mw = new RpcTracingMiddleware(null, new RpcTracingOptions { ActivitySource = source, InjectOutgoingContext = true });
        var meta = new RequestMeta(service: "svc", procedure: "proc");

        UnaryOutboundDelegate next = (req, ct) =>
        {
            Assert.True(req.Meta.TryGetHeader("traceparent", out var tp) && !string.IsNullOrEmpty(tp));
            return ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty)));
        };

        var res = await mw.InvokeAsync(new Request<ReadOnlyMemory<byte>>(meta, ReadOnlyMemory<byte>.Empty), TestContext.Current.CancellationToken, next);
        Assert.True(res.IsSuccess);
    }

    [Fact]
    public async Task InboundUnary_ExtractsParent_WhenPresent()
    {
        using var source = new ActivitySource("test.tracing");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test.tracing",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        var mw = new RpcTracingMiddleware(null, new RpcTracingOptions { ActivitySource = source, ExtractIncomingContext = true });

        using var parent = source.StartActivity("parent", ActivityKind.Server);
        var meta = new RequestMeta(service: "svc", procedure: "proc").WithHeader("traceparent", parent!.Id!);

        Activity? captured = null;
        UnaryInboundDelegate next = (req, ct) =>
        {
            captured = Activity.Current;
            return ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty)));
        };

        var res = await mw.InvokeAsync(new Request<ReadOnlyMemory<byte>>(meta, ReadOnlyMemory<byte>.Empty), TestContext.Current.CancellationToken, next);
        Assert.True(res.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(parent.TraceId, captured!.TraceId);
    }

    [Fact]
    public async Task SamplingProbabilityZero_DisablesActivity()
    {
        using var source = new ActivitySource("test.tracing");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test.tracing",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        var runtime = new TestRuntime();
        runtime.SetTraceSamplingProbability(0.0);
        var mw = new RpcTracingMiddleware(runtime, new RpcTracingOptions { ActivitySource = source });

        Activity? captured = null;
        UnaryOutboundDelegate next = (req, ct) =>
        {
            captured = Activity.Current;
            return ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty)));
        };

        var res = await mw.InvokeAsync(new Request<ReadOnlyMemory<byte>>(new RequestMeta(service: "svc"), ReadOnlyMemory<byte>.Empty), TestContext.Current.CancellationToken, next);
        Assert.True(res.IsSuccess);
        Assert.Null(captured);
    }
}
