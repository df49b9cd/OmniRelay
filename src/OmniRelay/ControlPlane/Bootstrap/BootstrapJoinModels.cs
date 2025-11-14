namespace OmniRelay.ControlPlane.Bootstrap;

/// <summary>Request payload for bootstrap join operations.</summary>
public sealed class BootstrapJoinRequest
{
    public string Token { get; set; } = string.Empty;

    public string? NodeId { get; set; }

    public string? RequestedRole { get; set; }
}

/// <summary>Response payload containing bootstrap materials.</summary>
public sealed class BootstrapJoinResponse
{
    public string ClusterId { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string CertificateData { get; init; } = string.Empty;

    public string? CertificatePassword { get; init; }

    public IReadOnlyList<string> SeedPeers { get; init; } = Array.Empty<string>();

    public DateTimeOffset ExpiresAt { get; init; }
}
