using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OmniRelay.Core;
using OmniRelay.Dispatcher;
using OmniRelay.Transport.Http;
using Xunit;

namespace OmniRelay.Tests.Transport.Http;

public class MetaHeadersTests
{
    [Fact(Timeout = 30000)]
    public async Task TtlAndDeadlineHeaders_RoundTripIntoRequestMeta()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}/");

        var options = new DispatcherOptions("meta");
        var httpInbound = new HttpInbound([baseAddress.ToString()]);
        options.AddLifecycle("http", httpInbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);
        dispatcher.Register(new UnaryProcedureSpec(
            "meta",
            "meta::echo",
            (request, _) =>
            {
                var ttlMs = request.Meta.TimeToLive?.TotalMilliseconds;
                var deadline = request.Meta.Deadline?.ToUniversalTime().ToString("O");
                var json = JsonSerializer.Serialize(new { ttlMs, deadline });
                var bytes = Encoding.UTF8.GetBytes(json);
                return ValueTask.FromResult(Hugo.Go.Ok(Response<ReadOnlyMemory<byte>>.Create(bytes, new ResponseMeta(encoding: "application/json"))));
            }));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);

        using var httpClient = new HttpClient { BaseAddress = baseAddress };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.Add(HttpTransportHeaders.Procedure, "meta::echo");
        request.Headers.Add(HttpTransportHeaders.TtlMs, "1500");
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5).ToString("O");
        request.Headers.Add(HttpTransportHeaders.Deadline, deadline);
        request.Content = new ByteArrayContent(Array.Empty<byte>());

        using var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.Equal(1500, doc.GetProperty("ttlMs").GetDouble(), precision: 0);
        Assert.Equal(deadline, doc.GetProperty("deadline").GetString());

        await dispatcher.StopAsync(ct);
    }
}
