using System.Text.Json;
using AwesomeAssertions;
using OmniRelay.Core;
using OmniRelay.Dispatcher;
using OmniRelay.IntegrationTests.Support;
using OmniRelay.Transport.Http;
using Xunit;
using static Hugo.Go;
using static OmniRelay.IntegrationTests.Support.TransportTestHelper;

namespace OmniRelay.IntegrationTests.Transport;

public sealed class HttpIntrospectionTests(ITestOutputHelper output) : TransportIntegrationTest(output)
{
    [Fact(Timeout = 30_000)]
    public async ValueTask IntrospectionEndpoint_ReportsDispatcherState()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}/");

        var options = new DispatcherOptions("inspect");
        var inbound = new HttpInbound([baseAddress.ToString()]);
        options.AddLifecycle("http-inbound", inbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);

        dispatcher.Register(new UnaryProcedureSpec(
            "inspect",
            "procedures::ping",
            (request, cancellationToken) =>
            {
                var response = Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty, new ResponseMeta(encoding: "application/json"));
                return ValueTask.FromResult(Ok(response));
            },
            encoding: "application/json"));

        var ct = TestContext.Current.CancellationToken;
        await using var host = await StartDispatcherAsync(nameof(IntrospectionEndpoint_ReportsDispatcherState), dispatcher, ct);
        await WaitForHttpEndpointReadyAsync(baseAddress, ct);

        using var client = new HttpClient { BaseAddress = baseAddress };
        using var response = await client.GetAsync("omnirelay/introspect", ct);
        response.IsSuccessStatusCode.Should().BeTrue($"HTTP {response.StatusCode}");

        await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: ct);

        var root = document.RootElement;
        root.GetProperty("service").GetString().Should().Be("inspect");
        root.GetProperty("status").GetString().Should().Be("Running");

        var procedures = root.GetProperty("procedures");
        var unaryProcedures = procedures.GetProperty("unary").EnumerateArray().ToArray();
        var procedure = unaryProcedures.Should().ContainSingle(element => element.GetProperty("name").GetString() == "procedures::ping").Which;
        procedure.GetProperty("encoding").GetString().Should().Be("application/json");
        procedures.GetProperty("oneway").GetArrayLength().Should().Be(0);
        procedures.GetProperty("stream").GetArrayLength().Should().Be(0);
        procedures.GetProperty("clientStream").GetArrayLength().Should().Be(0);
        procedures.GetProperty("duplex").GetArrayLength().Should().Be(0);

        var components = root.GetProperty("components").EnumerateArray().ToArray();
        components.Should().Contain(component =>
            string.Equals(component.GetProperty("name").GetString(), "http-inbound", StringComparison.OrdinalIgnoreCase) &&
            component.GetProperty("componentType").GetString()!.Contains(nameof(HttpInbound), StringComparison.Ordinal));

        var middleware = root.GetProperty("middleware");
        middleware.TryGetProperty("inboundUnary", out var inboundUnary).Should().BeTrue();
        inboundUnary.GetArrayLength().Should().Be(0);

        middleware.TryGetProperty("outboundUnary", out var outboundUnary).Should().BeTrue();
        outboundUnary.GetArrayLength().Should().Be(0);
    }
}
