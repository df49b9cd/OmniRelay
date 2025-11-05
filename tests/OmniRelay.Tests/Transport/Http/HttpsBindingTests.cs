using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using OmniRelay.Core;
using OmniRelay.Dispatcher;
using OmniRelay.Transport.Http;
using Xunit;

namespace OmniRelay.Tests.Transport.Http;

public class HttpsBindingTests
{
    [Fact(Timeout = 30000)]
    public async Task Https_WithCertificate_BindsAndServes()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"https://127.0.0.1:{port}/");

        using var cert = CreateSelfSigned("CN=omnirelay-test");

        var options = new DispatcherOptions("https");
        var tls = new HttpServerTlsOptions { Certificate = cert };
        var inbound = new HttpInbound([baseAddress.ToString()], serverTlsOptions: tls);
        options.AddLifecycle("https-inbound", inbound);

        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);
        dispatcher.Register(new UnaryProcedureSpec(
            "https",
            "ping",
            (req, _) => ValueTask.FromResult(Hugo.Go.Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty, new ResponseMeta())))));

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.StartAsync(ct);

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = static (_, _, _, _) => true
        };
        using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.Add(HttpTransportHeaders.Procedure, "ping");
        request.Content = new ByteArrayContent(Array.Empty<byte>());
        using var response = await httpClient.SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await dispatcher.StopAsync(ct);
    }

    [Fact(Timeout = 30000)]
    public async Task Https_WithoutCertificate_ThrowsOnStart()
    {
        var port = TestPortAllocator.GetRandomPort();
        var baseAddress = new Uri($"https://127.0.0.1:{port}/");

        var options = new DispatcherOptions("https");
        var inbound = new HttpInbound([baseAddress.ToString()]);
        options.AddLifecycle("https-inbound", inbound);
        var dispatcher = new OmniRelay.Dispatcher.Dispatcher(options);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await dispatcher.StartAsync(TestContext.Current.CancellationToken));
    }

    private static X509Certificate2 CreateSelfSigned(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        req.CertificateExtensions.Add(sanBuilder.Build());

        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return cert;
    }
}
