using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
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

public class RpcMetricsMiddlewareTests
{
    private sealed class Collector
    {
        public long Requests;
        public long Success;
        public long Failure;
        public List<double> Durations = new();
        public MeterListener Listener = new();

        public Collector(Meter meter, string prefix)
        {
            Listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter == meter)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            Listener.SetMeasurementEventCallback<long>((inst, value, tags, state) =>
            {
                if (inst.Name.EndsWith("requests")) Requests += value;
                else if (inst.Name.EndsWith("success")) Success += value;
                else if (inst.Name.EndsWith("failure")) Failure += value;
            });
            Listener.SetMeasurementEventCallback<double>((inst, value, tags, state) =>
            {
                if (inst.Name.EndsWith("duration")) Durations.Add(value);
            });
            Listener.Start();
        }
    }

    [Fact]
    public async Task Records_Success_And_Duration_For_Unary()
    {
        var meter = new Meter("test.rpc.metrics");
        var options = new RpcMetricsOptions { Meter = meter, MetricPrefix = "test.rpc" };
        var collector = new Collector(meter, options.MetricPrefix);
        var mw = new RpcMetricsMiddleware(options);

        var meta = new RequestMeta(service: "svc", procedure: "proc", transport: "http");
        UnaryOutboundDelegate next = (req, ct) => ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty)));
        var res = await mw.InvokeAsync(new Request<ReadOnlyMemory<byte>>(meta, ReadOnlyMemory<byte>.Empty), TestContext.Current.CancellationToken, next);

        Assert.True(res.IsSuccess);
    await Task.Delay(10, TestContext.Current.CancellationToken); // allow metrics to flush
        Assert.Equal(1, collector.Requests);
        Assert.Equal(1, collector.Success);
        Assert.Equal(0, collector.Failure);
        Assert.NotEmpty(collector.Durations);
        collector.Listener.Dispose();
        meter.Dispose();
    }

    [Fact]
    public async Task Records_Failure_For_Unary()
    {
        var meter = new Meter("test.rpc.metrics");
        var options = new RpcMetricsOptions { Meter = meter, MetricPrefix = "test.rpc" };
        var collector = new Collector(meter, options.MetricPrefix);
        var mw = new RpcMetricsMiddleware(options);

        var meta = new RequestMeta(service: "svc", procedure: "proc", transport: "http");
        UnaryOutboundDelegate next = (req, ct) => ValueTask.FromResult(Err<Response<ReadOnlyMemory<byte>>>(OmniRelayErrorAdapter.FromStatus(OmniRelayStatusCode.Unavailable, "fail", transport: "http")));
        var res = await mw.InvokeAsync(new Request<ReadOnlyMemory<byte>>(meta, ReadOnlyMemory<byte>.Empty), TestContext.Current.CancellationToken, next);

        Assert.True(res.IsFailure);
    await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.Equal(1, collector.Requests);
        Assert.Equal(0, collector.Success);
        Assert.Equal(1, collector.Failure);
        collector.Listener.Dispose();
        meter.Dispose();
    }
}
