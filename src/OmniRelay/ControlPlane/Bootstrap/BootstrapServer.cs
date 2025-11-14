using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using OmniRelay.ControlPlane.Security;

namespace OmniRelay.ControlPlane.Bootstrap;

/// <summary>Handles join requests by validating tokens and packaging bootstrap bundles.</summary>
public sealed class BootstrapServer
{
    private readonly BootstrapServerOptions _options;
    private readonly BootstrapTokenService _tokenService;
    private readonly TransportTlsManager _tlsManager;
    private readonly ILogger<BootstrapServer> _logger;

    public BootstrapServer(
        BootstrapServerOptions options,
        BootstrapTokenService tokenService,
        TransportTlsManager tlsManager,
        ILogger<BootstrapServer> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _tlsManager = tlsManager ?? throw new ArgumentNullException(nameof(tlsManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public BootstrapJoinResponse Join(BootstrapJoinRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Token))
        {
            throw new BootstrapServerException("join-token-missing", "A join token must be supplied.");
        }

        var validation = _tokenService.ValidateToken(request.Token, _options.ClusterId);
        if (!validation.IsValid)
        {
            throw new BootstrapServerException("join-token-invalid", validation.FailureReason ?? "Token validation failed.");
        }

        var role = validation.Role;
        if (!string.IsNullOrWhiteSpace(request.RequestedRole) &&
            !string.Equals(request.RequestedRole, role, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Join request for node {NodeId} attempted to override role '{RequestedRole}' (token role '{TokenRole}').",
                request.NodeId,
                request.RequestedRole,
                role);
        }

        using var certificate = _tlsManager.GetCertificate();
        var exported = ExportCertificate(certificate, _options.BundlePassword);
        var response = new BootstrapJoinResponse
        {
            ClusterId = validation.ClusterId,
            Role = role,
            CertificateData = exported,
            CertificatePassword = _options.BundlePassword,
            SeedPeers = _options.SeedPeers.ToArray(),
            ExpiresAt = validation.ExpiresAt
        };

        _logger.LogInformation("Issued bootstrap bundle for cluster {ClusterId} role {Role} (token {TokenId}).", validation.ClusterId, role, validation.TokenId);
        return response;
    }

    private static string ExportCertificate(X509Certificate2 certificate, string? password)
    {
        var bytes = certificate.Export(X509ContentType.Pfx, password);
        return Convert.ToBase64String(bytes);
    }
}

internal sealed class BootstrapServerException(string Code, string Message) : Exception(Message)
{
    public string ErrorCode { get; } = Code;
}
