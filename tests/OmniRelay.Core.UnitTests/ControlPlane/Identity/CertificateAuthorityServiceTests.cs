using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OmniRelay.ControlPlane.Identity;
using OmniRelay.Core.UnitTests.ControlPlane.ControlProtocol;
using OmniRelay.Protos.Ca;
using Xunit;

namespace OmniRelay.Core.UnitTests.ControlPlane.Identity;

public sealed class CertificateAuthorityServiceTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public async Task SubmitCsr_IssuesLeafCertificateAndTrustBundle()
    {
        var service = new CertificateAuthorityService(
            Options.Create(new CertificateAuthorityOptions { LeafLifetime = TimeSpan.FromHours(2) }),
            NullLogger<CertificateAuthorityService>.Instance);

        var response = await service.SubmitCsr(new CsrRequest { NodeId = "agent-1" }, new TestServerCallContext(CancellationToken.None));

        response.ShouldNotBeNull();
        response.ExpiresAt.ShouldNotBeNullOrWhiteSpace();

        var pem = PemEncoding.Write("CERTIFICATE", response.Certificate.ToByteArray());
        var leaf = X509Certificate2.CreateFromPem(pem);
        leaf.Subject.ShouldContain("agent-1", Case.Insensitive);

        var trust = response.TrustBundle.ToByteArray();
        trust.ShouldNotBeEmpty();
    }
}
