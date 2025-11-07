using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OmniRelay.Tests.Support;

internal static class TestCertificateFactory
{
    private const string DefaultPassword = "omnirelay-test-cert";

    public static X509Certificate2 CreateLoopbackCertificate(string subjectName)
    {
        if (string.IsNullOrWhiteSpace(subjectName))
        {
            throw new ArgumentException("Subject name is required.", nameof(subjectName));
        }

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var eku = new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.1") // Server Authentication
        };
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        var export = certificate.Export(X509ContentType.Pfx, DefaultPassword);
        return X509CertificateLoader.LoadPkcs12(
            export,
            DefaultPassword,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
    }
}
