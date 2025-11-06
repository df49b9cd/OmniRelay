using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using OmniRelay.Transport.Http;
using Xunit;

namespace OmniRelay.Tests.Transport.Http;

public sealed class HttpTransportMetricsTests
{
    [Fact]
    public void CreateBaseTags_WithHttp3_AddsNetworkMetadata()
    {
        var tags = HttpTransportMetrics.CreateBaseTags("svc", "svc::call", "POST", "HTTP/3");
        var lookup = tags.ToDictionary(static pair => pair.Key, static pair => pair.Value);

        Assert.Equal("http", lookup["rpc.system"]);
        Assert.Equal("svc", lookup["rpc.service"]);
        Assert.Equal("svc::call", lookup["rpc.procedure"]);
        Assert.Equal("POST", lookup["http.request.method"]);
        Assert.Equal("HTTP/3", lookup["rpc.protocol"]);
        Assert.Equal("http", lookup["network.protocol.name"]);
        Assert.Equal("3", lookup["network.protocol.version"]);
        Assert.Equal("quic", lookup["network.transport"]);
    }

    [Fact]
    public void AppendOutcome_WithStatusCode_AppendsOutcomeTags()
    {
        var baseTags = new[] { KeyValuePair.Create<string, object?>("rpc.system", "http") };
        var tagged = HttpTransportMetrics.AppendOutcome(baseTags, StatusCodes.Status503ServiceUnavailable, "error");
        var lookup = tagged.ToDictionary(static pair => pair.Key, static pair => pair.Value);

        Assert.Equal("http", lookup["rpc.system"]);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, lookup["http.response.status_code"]);
        Assert.Equal("error", lookup["outcome"]);
    }

    [Fact]
    public void AppendObservedProtocol_AppendsValueAndPreservesBaseTags()
    {
        var baseTags = new[] { KeyValuePair.Create<string, object?>("rpc.system", "http") };

        var unchanged = HttpTransportMetrics.AppendObservedProtocol(baseTags, "  ");
        Assert.Same(baseTags, unchanged);

        var enriched = HttpTransportMetrics.AppendObservedProtocol(baseTags, "HTTP/2");
        Assert.NotSame(baseTags, enriched);
        var lookup = enriched.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        Assert.Equal("http", lookup["rpc.system"]);
        Assert.Equal("HTTP/2", lookup["http.observed_protocol"]);
    }
}
