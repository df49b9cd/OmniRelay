using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OmniRelay.Tests.Support;

internal static class TestCertificateFactory
{
    private const string DevCertRelativePath = "tests/TestSupport/devcert.pfx";
    private const string DevCertPassword = "applepie";

    public static X509Certificate2 CreateLoopbackCertificate(string subjectName)
    {
        if (string.IsNullOrWhiteSpace(subjectName))
        {
            throw new ArgumentException("Subject name is required.", nameof(subjectName));
        }

        var devCert = TryLoadDeveloperCertificate();
        if (devCert is not null)
        {
            return devCert;
        }

        return CreateEphemeralLoopbackCertificate(subjectName);
    }

    private static X509Certificate2? TryLoadDeveloperCertificate()
    {
        try
        {
            var root = TryResolveRepositoryRoot();
            if (root is null)
            {
                return null;
            }

            var candidatePath = Path.Combine(root, DevCertRelativePath);
            if (!File.Exists(candidatePath))
            {
                return null;
            }

            return X509CertificateLoader.LoadPkcs12FromFile(candidatePath, DevCertPassword, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryResolveRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "OmniRelay.slnx")) ||
                File.Exists(Path.Combine(directory, "OmniRelay.sln")))
            {
                return directory;
            }

            var parent = Directory.GetParent(directory);
            if (parent is null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return null;
    }

    private static X509Certificate2 CreateEphemeralLoopbackCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var eku = new OidCollection
        {
            new("1.3.6.1.5.5.7.3.1") // Server Authentication
        };
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }
}
