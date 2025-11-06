using System;
using System.Net;
using System.Net.Http;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using OmniRelay.Core;
using OmniRelay.Dispatcher;
using OmniRelay.Transport.Http;
using Xunit;

namespace OmniRelay.Tests.Transport.Http;

public class Http3FallbackErrorTests
{
    [Fact(Timeout = 45_000)]
    public async Task MissingProcedure_Http3AndHttp2ResponsesMatch()
    {
        if (!QuicListener.IsSupported)
        {
            return;
        }

        using var certificate = CreateSelfSigned("CN=omnirelay-http3-errors");

        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"https://127.0.0.1:{port}/");

        var runtime = new HttpServerRuntimeOptions { EnableHttp3 = true };
        var tls = new HttpServerTlsOptions { Certificate = certificate };

        var options = new DispatcherOptions("http3-errors");
        var inbound = new HttpInbound([baseAddress.ToString()], serverRuntimeOptions: runtime, serverTlsOptions: tls);
        options.AddLifecycle("http3-errors-inbound", inbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);
        dispatcher.Register(new UnaryProcedureSpec(
            "http3-errors",
            "noop::ok",
            (request, _) => ValueTask.FromResult(Hugo.Go.Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty, new ResponseMeta())))));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);

        try
        {
            var http3Snapshot = await CaptureErrorSnapshotAsync(CreateHttp3Client(baseAddress, HttpVersionPolicy.RequestVersionExact), ct);
            var http2Snapshot = await CaptureErrorSnapshotAsync(CreateHttp2Client(baseAddress), ct);

            Assert.Equal(http3Snapshot, http2Snapshot);
        }
        finally
        {
            await dispatcher.StopAsync(ct);
        }
    }

    [Fact(Timeout = 45_000)]
    public async Task MissingProcedure_Http3FallbackToHttp2MatchesPayload()
    {
        if (!QuicListener.IsSupported)
        {
            return;
        }

        using var certificate = CreateSelfSigned("CN=omnirelay-http3-fallback");

        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"https://127.0.0.1:{port}/");

        var runtime = new HttpServerRuntimeOptions { EnableHttp3 = false };
        var tls = new HttpServerTlsOptions { Certificate = certificate };

        var options = new DispatcherOptions("http3-fallback-errors");
        var inbound = new HttpInbound([baseAddress.ToString()], serverRuntimeOptions: runtime, serverTlsOptions: tls);
        options.AddLifecycle("http3-fallback-errors-inbound", inbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);
        dispatcher.Register(new UnaryProcedureSpec(
            "http3-fallback-errors",
            "noop::ok",
            (request, _) => ValueTask.FromResult(Hugo.Go.Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty, new ResponseMeta())))));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);

        try
        {
            var fallbackSnapshot = await CaptureErrorSnapshotAsync(CreateHttp3Client(baseAddress, HttpVersionPolicy.RequestVersionOrLower), ct);
            var http2Snapshot = await CaptureErrorSnapshotAsync(CreateHttp2Client(baseAddress), ct);

            Assert.Equal(HttpStatusCode.BadRequest, fallbackSnapshot.StatusCode);
            Assert.Equal(http2Snapshot, fallbackSnapshot);
            Assert.Equal("HTTP/2", fallbackSnapshot.ProtocolHeader);
        }
        finally
        {
            await dispatcher.StopAsync(ct);
        }
    }

    private static async Task<ErrorSnapshot> CaptureErrorSnapshotAsync(HttpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/");
            request.Content = new ByteArrayContent(Array.Empty<byte>());

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            response.Headers.TryGetValues(HttpTransportHeaders.Status, out var statusValues);
            response.Headers.TryGetValues(HttpTransportHeaders.ErrorMessage, out var messageValues);
            response.Headers.TryGetValues(HttpTransportHeaders.Transport, out var transportValues);
            response.Headers.TryGetValues(HttpTransportHeaders.Protocol, out var protocolValues);

            string? jsonStatus = null;
            string? jsonMessage = null;
            if (!string.IsNullOrEmpty(payload))
            {
                using var document = JsonDocument.Parse(payload);
                if (document.RootElement.TryGetProperty("status", out var statusProperty))
                {
                    jsonStatus = statusProperty.GetString();
                }

                if (document.RootElement.TryGetProperty("message", out var messageProperty))
                {
                    jsonMessage = messageProperty.GetString();
                }
            }

            return new ErrorSnapshot(
                response.StatusCode,
                statusValues is null ? null : string.Join(",", statusValues),
                messageValues is null ? null : string.Join(",", messageValues),
                transportValues is null ? null : string.Join(",", transportValues),
                protocolValues is null ? null : string.Join(",", protocolValues),
                jsonStatus,
                jsonMessage,
                response.Version);
        }
    }

    private static HttpClient CreateHttp3Client(Uri baseAddress, HttpVersionPolicy policy)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            SslOptions =
            {
                RemoteCertificateValidationCallback = static (_, _, _, _) => true,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ApplicationProtocols =
                [
                    SslApplicationProtocol.Http3,
                    SslApplicationProtocol.Http2,
                    SslApplicationProtocol.Http11
                ]
            }
        };

        return new HttpClient(handler)
        {
            BaseAddress = baseAddress,
            DefaultRequestVersion = HttpVersion.Version30,
            DefaultVersionPolicy = policy
        };
    }

    private static HttpClient CreateHttp2Client(Uri baseAddress)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            SslOptions =
            {
                RemoteCertificateValidationCallback = static (_, _, _, _) => true,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ApplicationProtocols =
                [
                    SslApplicationProtocol.Http2,
                    SslApplicationProtocol.Http11
                ]
            }
        };

        return new HttpClient(handler)
        {
            BaseAddress = baseAddress,
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
    }

    private static X509Certificate2 CreateSelfSigned(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }

    private sealed record ErrorSnapshot(
        HttpStatusCode StatusCode,
        string? StatusHeader,
        string? MessageHeader,
        string? TransportHeader,
        string? ProtocolHeader,
        string? JsonStatus,
        string? JsonMessage,
        Version HttpVersion);
}
