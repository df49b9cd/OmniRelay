using System.Net;
using System.Net.Mime;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OmniRelay.Core;
using OmniRelay.Dispatcher;
using OmniRelay.Dispatcher.Config;
using OmniRelay.Transport.Http;
using Xunit;
using static Hugo.Go;

namespace OmniRelay.IntegrationTests;

public class HttpDispatcherHostIntegrationTests
{
    [Fact(Timeout = 30_000)]
    public async ValueTask HttpInbound_ConfiguredViaHost_RoundtripsUnaryRequest()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseUrl = $"http://127.0.0.1:{port}";
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["omnirelay:service"] = "integration-http",
            ["omnirelay:inbounds:http:0:urls:0"] = baseUrl
        });

        builder.Services.AddLogging();
        builder.Services.AddOmniRelayDispatcherFromConfiguration(builder.Configuration.GetSection("omnirelay"));

        using var host = builder.Build();
        var dispatcher = host.Services.GetRequiredService<Dispatcher.Dispatcher>();

        dispatcher.Register(new UnaryProcedureSpec(
            "integration-http",
            "integration::ping",
            static (request, _) =>
            {
                var responseBytes = "integration-http-response"u8.ToArray();
                var responseMeta = new ResponseMeta(encoding: MediaTypeNames.Text.Plain);
                return ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(responseBytes, responseMeta)));
            }));

        var ct = TestContext.Current.CancellationToken;
        await host.StartAsync(ct);

        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            httpClient.DefaultRequestHeaders.Add(HttpTransportHeaders.Procedure, "integration::ping");
            httpClient.DefaultRequestHeaders.Add(HttpTransportHeaders.Transport, "http");

            using var content = new ByteArrayContent("ping"u8.ToArray());
            using var response = await httpClient.PostAsync("/", content, ct);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync(ct);
            body.Should().Be("integration-http-response");

            response.Headers.TryGetValues(HttpTransportHeaders.Encoding, out var encodingValues).Should().BeTrue();
            encodingValues.Should().Contain(MediaTypeNames.Text.Plain);
            response.Headers.TryGetValues(HttpTransportHeaders.Transport, out var transportValues).Should().BeTrue();
            transportValues.Should().Contain("http");
        }
        finally
        {
            await host.StopAsync(CancellationToken.None);
        }
    }
}
