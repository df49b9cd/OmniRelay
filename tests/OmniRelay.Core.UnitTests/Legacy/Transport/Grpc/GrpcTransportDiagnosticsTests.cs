using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Grpc.Core;
using OmniRelay.Transport.Grpc;
using Xunit;

namespace OmniRelay.Tests.Transport.Grpc;

public sealed class GrpcTransportDiagnosticsTests
{
    [Fact]
    public void StartClientActivity_NoListener_ReturnsNull()
    {
        var activity = GrpcTransportDiagnostics.StartClientActivity(
            remoteService: "svc",
            procedure: "svc::Unary",
            address: new Uri("https://example.test:5001"),
            operation: "unary");

        Assert.Null(activity);
    }

    [Fact]
    public void StartClientActivity_WithListener_PopulatesRpcAndNetworkTags()
    {
        var started = new List<Activity>();
        using var listener = CreateListener(started);

        using var activity = GrpcTransportDiagnostics.StartClientActivity(
            remoteService: "backend",
            procedure: "backend::Echo",
            address: new Uri("https://example.test:8443/echo"),
            operation: "unary");

        Assert.NotNull(activity);
        Assert.Equal("grpc", (string?)activity!.GetTagItem("rpc.system"));
        Assert.Equal("backend", (string?)activity.GetTagItem("rpc.service"));
        Assert.Equal("backend::Echo", (string?)activity.GetTagItem("rpc.method"));
        Assert.Equal("example.test", (string?)activity.GetTagItem("net.peer.name"));
        Assert.Equal(8443, (int)activity.GetTagItem("net.peer.port")!);

        using var ipActivity = GrpcTransportDiagnostics.StartClientActivity(
            remoteService: "backend",
            procedure: "backend::Echo",
            address: new Uri("https://127.0.0.1:9443/echo"),
            operation: "unary");

        Assert.NotNull(ipActivity);
        Assert.Equal("127.0.0.1", (string?)ipActivity!.GetTagItem("net.peer.ip"));
        Assert.Equal(9443, (int)ipActivity.GetTagItem("net.peer.port")!);
    }

    [Fact]
    public void SetStatusAndRecordException_UpdateActivityState()
    {
        var started = new List<Activity>();
        using var listener = CreateListener(started);

        using var activity = GrpcTransportDiagnostics.StartClientActivity(
            remoteService: "svc",
            procedure: "svc::Unary",
            address: new Uri("https://example.test:5900"),
            operation: "unary");

        Assert.NotNull(activity);

        GrpcTransportDiagnostics.SetStatus(activity, StatusCode.OK);
        Assert.Equal(ActivityStatusCode.Ok, activity!.Status);

        var ex = new InvalidOperationException("boom");
        GrpcTransportDiagnostics.RecordException(activity, ex, StatusCode.Internal);

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        var exceptionEvent = Assert.Single(activity.Events, evt => evt.Name == "exception");
        Assert.Contains(exceptionEvent.Tags!, tag => tag.Key == "exception.message" && Equals(tag.Value, "boom"));
    }

    [Fact]
    public void ParseHttpProtocol_HandlesHttpAndCustomValues()
    {
        var parseMethod = typeof(GrpcTransportDiagnostics).GetMethod(
            "ParseHttpProtocol",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(parseMethod);

        var http3 = InvokeParse(parseMethod, "HTTP/3.0");
        Assert.Equal(("http", "3.0"), http3);

        var http2 = InvokeParse(parseMethod, "HTTP/2");
        Assert.Equal(("http", "2"), http2);

        var custom = InvokeParse(parseMethod, "grpc");
        Assert.Equal(("grpc", (string?)null), custom);

        var missing = InvokeParse(parseMethod, null);
        Assert.Equal(((string?)null, (string?)null), missing);
    }

    [Fact]
    public void ExtractParentContext_ReturnsNullForInvalidTraceParent()
    {
        var extract = typeof(GrpcTransportDiagnostics).GetMethod(
            "ExtractParentContext",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(extract);

        var metadata = new Metadata { { "traceparent", "not-a-valid-trace" } };
        var context = (ActivityContext?)extract!.Invoke(null, [metadata]);
        Assert.False(context.HasValue);
    }

    [Fact]
    public void ExtractParentContext_ReturnsContextForValidTraceParent()
    {
        var extract = typeof(GrpcTransportDiagnostics).GetMethod(
            "ExtractParentContext",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(extract);

        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var metadata = new Metadata
        {
            { "traceparent", $"00-{traceId}-{spanId}-01" },
            { "tracestate", "congo=t61rcWkgMzE" }
        };

        var context = (ActivityContext?)extract!.Invoke(null, [metadata]);
        Assert.True(context.HasValue);
        Assert.Equal(traceId, context.Value.TraceId);
        Assert.Equal(spanId, context.Value.SpanId);
        Assert.Equal("congo=t61rcWkgMzE", context.Value.TraceState);
    }

    private static (string? Name, string? Version) InvokeParse(MethodInfo parseMethod, string? protocol)
    {
        var result = parseMethod.Invoke(null, [protocol]);
        Assert.NotNull(result);
        return ((string? Name, string? Version))result!;
    }

    private static ActivityListener CreateListener(ICollection<Activity> started)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(
                source.Name,
                GrpcTransportDiagnostics.ActivitySourceName,
                StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => started.Add(activity!)
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }

}
