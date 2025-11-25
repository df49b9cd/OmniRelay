using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Hugo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniRelay.Protos.Ca;
using static Hugo.Go;

namespace OmniRelay.ControlPlane.Identity;

/// <summary>In-process CA service for MeshKit agents (WORK-007): issues short-lived leaf certs and exposes the trust bundle.</summary>
public sealed partial class CertificateAuthorityService : CertificateAuthority.CertificateAuthorityBase, IDisposable
{
    private readonly CertificateAuthorityOptions _options;
    private readonly ILogger<CertificateAuthorityService> _logger;
    private readonly object _sync = new();
    private CaMaterial? _material;
    private DateTimeOffset _lastRootCheck = DateTimeOffset.MinValue;
    private bool _disposed;

    public CertificateAuthorityService(IOptions<CertificateAuthorityOptions> options, ILogger<CertificateAuthorityService> logger)
    {
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<CertResponse> SubmitCsr(CsrRequest request, ServerCallContext context)
    {
        var result = await Task.Run(() => IssueAsync(request, context.CancellationToken), context.CancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            throw ToRpcException(result.Error!);
        }

        return result.Value;
    }

    public override Task<TrustBundleResponse> TrustBundle(TrustBundleRequest request, ServerCallContext context)
    {
        var material = GetMaterial();
        if (material.IsFailure)
        {
            throw ToRpcException(material.Error!);
        }

        return Task.FromResult(new TrustBundleResponse
        {
            TrustBundle = Google.Protobuf.ByteString.CopyFrom(material.Value.TrustBundle)
        });
    }

    private Result<CertResponse> IssueAsync(CsrRequest request, CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return Err<CertResponse>(Error.From("Certificate authority has been disposed.", "ca.disposed"));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Err<CertResponse>(Error.Canceled("CSR request canceled", cancellationToken));
        }

        if (string.IsNullOrWhiteSpace(request.NodeId))
        {
            return Err<CertResponse>(Error.From("node_id is required", "ca.node_id.required"));
        }

        var material = GetMaterial();
        if (material.IsFailure)
        {
            return material.CastFailure<CertResponse>();
        }

        var csrInfo = ParseCsr(request);
        if (csrInfo.IsFailure)
        {
            return csrInfo.CastFailure<CertResponse>();
        }

        var binding = ValidateIdentityBinding(csrInfo.Value, request.NodeId, _options.RequireNodeBinding);
        if (binding.IsFailure)
        {
            return binding.CastFailure<CertResponse>();
        }

        var trustDomain = ValidateTrustDomain(csrInfo.Value);
        if (trustDomain.IsFailure)
        {
            return trustDomain.CastFailure<CertResponse>();
        }

        var issuedAt = DateTimeOffset.UtcNow;
        var notAfter = issuedAt + _options.LeafLifetime;

        var issueResult = IssueLeaf(material.Value.Root, csrInfo.Value, request.NodeId, issuedAt, notAfter);
        if (issueResult.IsFailure)
        {
            return issueResult.CastFailure<CertResponse>();
        }

        var leaf = issueResult.Value;
        var chainBytes = Concat(leaf, material.Value.Root);
        var renewAfter = issuedAt + TimeSpan.FromTicks((long)(_options.LeafLifetime.Ticks * _options.RenewalWindow));
        if (renewAfter > notAfter)
        {
            renewAfter = notAfter;
        }

        var response = new CertResponse
        {
            Certificate = Google.Protobuf.ByteString.CopyFrom(leaf.Export(X509ContentType.Cert)),
            CertificateChain = Google.Protobuf.ByteString.CopyFrom(chainBytes),
            TrustBundle = Google.Protobuf.ByteString.CopyFrom(material.Value.TrustBundle),
            ExpiresAt = leaf.NotAfter.ToUniversalTime().ToString("O"),
            RenewAfter = renewAfter.ToUniversalTime().ToString("O"),
            IssuedAt = issuedAt.ToUniversalTime().ToString("O"),
            Subject = csrInfo.Value.CommonName ?? leaf.SubjectName.Name ?? string.Empty
        };
        response.SanDns.AddRange(csrInfo.Value.Sans.DnsNames);
        response.SanUri.AddRange(csrInfo.Value.Sans.Uris);
        if (response.SanDns.Count == 0 && !string.IsNullOrWhiteSpace(request.NodeId))
        {
            response.SanDns.Add(request.NodeId);
        }

        CaLog.Issued(_logger, request.NodeId, response.Subject ?? string.Empty, leaf.NotAfter);

        return Ok(response);
    }

    private Result<CaMaterial> GetMaterial()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return Err<CaMaterial>(Error.From("Certificate authority has been disposed.", "ca.disposed"));
            }

            if (_material is null || ShouldReloadRoot(_material))
            {
                var reload = CreateOrLoadRoot();
                if (reload.IsFailure)
                {
                    return reload;
                }

                _material?.Root.Dispose();

                _material = reload.Value;
                if (!string.IsNullOrWhiteSpace(_options.RootPfxPath))
                {
                    CaLog.RootReloaded(_logger, _options.RootPfxPath!);
                }
            }

            return Ok(_material);
        }
    }

    private bool ShouldReloadRoot(CaMaterial current)
    {
        if (string.IsNullOrWhiteSpace(_options.RootPfxPath))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (_options.RootReloadInterval > TimeSpan.Zero && now - _lastRootCheck < _options.RootReloadInterval)
        {
            return false;
        }

        _lastRootCheck = now;
        if (!File.Exists(_options.RootPfxPath))
        {
            return false;
        }

        var lastWrite = File.GetLastWriteTimeUtc(_options.RootPfxPath);
        return lastWrite > current.LastWrite;
    }

    private Result<CaMaterial> CreateOrLoadRoot()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_options.RootPfxPath) && File.Exists(_options.RootPfxPath))
            {
                var persisted = X509CertificateLoader.LoadPkcs12FromFile(_options.RootPfxPath, _options.RootPfxPassword, X509KeyStorageFlags.Exportable);
                var persistedBundle = ExportPem(persisted);
                var lastWrite = File.GetLastWriteTimeUtc(_options.RootPfxPath);
                return Ok(new CaMaterial(persisted, persistedBundle, lastWrite));
            }

            using var rsa = RSA.Create(3072);
            var dn = new X500DistinguishedName(_options.IssuerName);
            var req = new CertificateRequest(dn, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

            var now = DateTimeOffset.UtcNow.AddMinutes(-5);
            var root = req.CreateSelfSigned(now, now.Add(_options.RootLifetime));

            if (!string.IsNullOrWhiteSpace(_options.RootPfxPath))
            {
                var pfx = root.Export(X509ContentType.Pfx, _options.RootPfxPassword);
                var directory = Path.GetDirectoryName(_options.RootPfxPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(_options.RootPfxPath!, pfx);
            }

            var trustBundle = ExportPem(root);
            return Ok(new CaMaterial(root, trustBundle, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Err<CaMaterial>(Error.FromException(ex));
        }
    }

    private static Result<X509Certificate2> IssueLeaf(
        X509Certificate2 issuer,
        CsrInfo csr,
        string nodeId,
        DateTimeOffset issuedAt,
        DateTimeOffset notAfter)
    {
        return Result.Try(() =>
        {
            var req = csr.Request;
            EnsureLeafExtensions(req, nodeId);

            var serial = RandomNumberGenerator.GetBytes(16);
            using var issuerKey = issuer.GetRSAPrivateKey() ?? throw new InvalidOperationException("CA certificate is missing a private key.");
            var generator = X509SignatureGenerator.CreateForRSA(issuerKey, RSASignaturePadding.Pkcs1);
            var cert = req.Create(issuer.SubjectName, generator, issuedAt.AddMinutes(-1).UtcDateTime, notAfter.UtcDateTime, serial);
            return cert;
        });
    }

    private static byte[] Concat(params X509Certificate2[] certs)
    {
        using var ms = new MemoryStream();
        foreach (var cert in certs)
        {
            var raw = cert.Export(X509ContentType.Cert);
            ms.Write(raw, 0, raw.Length);
        }
        return ms.ToArray();
    }

    private static byte[] ExportPem(X509Certificate2 cert)
    {
        using var writer = new StringWriter();
        writer.WriteLine("-----BEGIN CERTIFICATE-----");
        writer.WriteLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
        writer.WriteLine("-----END CERTIFICATE-----");
        return System.Text.Encoding.UTF8.GetBytes(writer.ToString());
    }

    private static Result<CsrInfo> ParseCsr(CsrRequest request)
    {
        if (request.Csr.IsEmpty)
        {
            return Err<CsrInfo>(Error.From("csr is required", "ca.csr.required"));
        }

        try
        {
            var bytes = request.Csr.ToByteArray();
            var csr = CertificateRequest.LoadSigningRequest(bytes, HashAlgorithmName.SHA256, out var bytesRead);
            if (bytesRead != bytes.Length)
            {
                return Err<CsrInfo>(Error.From("csr contains trailing data", "ca.csr.trailing"));
            }

            var sans = ExtractSubjectAlternativeNames(csr);
            var cn = GetCommonName(csr.SubjectName);
            return Ok(new CsrInfo(csr, sans, cn));
        }
        catch (Exception ex)
        {
            return Err<CsrInfo>(Error.FromException(ex).WithCode("ca.csr.invalid"));
        }
    }

    private static Result<Unit> ValidateIdentityBinding(CsrInfo csr, string nodeId, bool required)
    {
        if (!required)
        {
            return Ok(Unit.Value);
        }

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return Err<Unit>(Error.From("node_id is required", "ca.node_id.required"));
        }

        var match =
            (!string.IsNullOrWhiteSpace(csr.CommonName) && string.Equals(csr.CommonName, nodeId, StringComparison.OrdinalIgnoreCase)) ||
            csr.Sans.DnsNames.Any(dns => string.Equals(dns, nodeId, StringComparison.OrdinalIgnoreCase)) ||
            csr.Sans.Uris.Any(uri => string.Equals(uri, nodeId, StringComparison.OrdinalIgnoreCase));

        return match
            ? Ok(Unit.Value)
            : Err<Unit>(Error.From($"CSR does not bind to node_id '{nodeId}'", "ca.identity.mismatch")
                .WithMetadata("node_id", nodeId)
                .WithMetadata("cn", csr.CommonName ?? string.Empty)
                .WithMetadata("san.dns", string.Join(',', csr.Sans.DnsNames))
                .WithMetadata("san.uri", string.Join(',', csr.Sans.Uris)));
    }

    private Result<Unit> ValidateTrustDomain(CsrInfo csr)
    {
        if (string.IsNullOrWhiteSpace(_options.TrustDomain))
        {
            return Ok(Unit.Value);
        }

        var mismatched = csr.Sans.Uris
            .Where(uri => uri.StartsWith("spiffe://", StringComparison.OrdinalIgnoreCase))
            .Where(uri => !uri.StartsWith(_options.TrustDomain, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return mismatched.Length == 0
            ? Ok(Unit.Value)
            : Err<Unit>(Error.From("SPIFFE trust domain mismatch.", "ca.trust_domain.mismatch")
                .WithMetadata("expected", _options.TrustDomain)
                .WithMetadata("found", string.Join(',', mismatched)));
    }

    private static void EnsureLeafExtensions(CertificateRequest request, string nodeId)
    {
        if (!request.CertificateExtensions.Any(ext => ext.Oid?.Value == "2.5.29.19"))
        {
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        }

        if (!request.CertificateExtensions.Any(ext => ext.Oid?.Value == "2.5.29.15"))
        {
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        }

        if (!request.CertificateExtensions.Any(ext => ext.Oid?.Value == "2.5.29.17"))
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(nodeId);
            request.CertificateExtensions.Add(sanBuilder.Build());
        }

        if (!request.CertificateExtensions.Any(ext => ext.Oid?.Value == "2.5.29.37"))
        {
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
            {
                new(Oids.ServerAuth),
                new(Oids.ClientAuth)
            }, false));
        }

        if (!request.CertificateExtensions.Any(ext => ext.Oid?.Value == "2.5.29.14"))
        {
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        }
    }

    private static SubjectAlternativeNameData ExtractSubjectAlternativeNames(CertificateRequest request)
    {
        foreach (var extension in request.CertificateExtensions)
        {
            if (extension.Oid?.Value == "2.5.29.17")
            {
                return ParseSubjectAlternativeName(extension.RawData);
            }
        }

        return new SubjectAlternativeNameData(Array.Empty<string>(), Array.Empty<string>());
    }

    private static SubjectAlternativeNameData ParseSubjectAlternativeName(ReadOnlyMemory<byte> rawData)
    {
        var dns = new List<string>();
        var uris = new List<string>();
        var reader = new AsnReader(rawData, AsnEncodingRules.DER);
        var seq = reader.ReadSequence();
        while (seq.HasData)
        {
            var tag = seq.PeekTag();
            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                dns.Add(seq.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 2)));
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 6)))
            {
                uris.Add(seq.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 6)));
            }
            else
            {
                seq.ReadEncodedValue();
            }
        }

        return new SubjectAlternativeNameData(dns.ToArray(), uris.ToArray());
    }

    private static string? GetCommonName(X500DistinguishedName subject)
    {
        var name = subject.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var parts = name.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                return part[3..];
            }
        }

        return null;
    }

    private static RpcException ToRpcException(Error error)
    {
        var metadata = new Metadata();
        if (!string.IsNullOrWhiteSpace(error.Code))
        {
            metadata.Add("error-code", error.Code);
        }

        if (error.Metadata is not null)
        {
            foreach (var pair in error.Metadata)
            {
                if (pair.Value is string value)
                {
                    metadata.Add(pair.Key, value);
                }
            }
        }

        var status = new Status(StatusCode.FailedPrecondition, error.Message ?? "certificate authority error");
        return new RpcException(status, metadata);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _material?.Root.Dispose();
    }

    private sealed record CaMaterial(X509Certificate2 Root, byte[] TrustBundle, DateTimeOffset LastWrite);

    private sealed record SubjectAlternativeNameData(string[] DnsNames, string[] Uris);

    private sealed record CsrInfo(CertificateRequest Request, SubjectAlternativeNameData Sans, string? CommonName);

    private static partial class CaLog
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "CA issued certificate for node_id={NodeId} subject={Subject} expires={Expires}")]
        public static partial void Issued(ILogger logger, string nodeId, string subject, DateTimeOffset expires);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "CA root reloaded from {Path}")]
        public static partial void RootReloaded(ILogger logger, string path);
    }

    private static class Oids
    {
        public const string ServerAuth = "1.3.6.1.5.5.7.3.1";
        public const string ClientAuth = "1.3.6.1.5.5.7.3.2";
    }
}
