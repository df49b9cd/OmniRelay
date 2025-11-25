namespace OmniRelay.ControlPlane.Identity;

public sealed class CertificateAuthorityOptions
{
    /// <summary>Distinguished name for the root CA.</summary>
    public string IssuerName { get; set; } = "CN=OmniRelay MeshKit CA";

    /// <summary>Lifetime for the root certificate.</summary>
    public TimeSpan RootLifetime { get; set; } = TimeSpan.FromDays(365);

    /// <summary>Lifetime for issued leaf certificates.</summary>
    public TimeSpan LeafLifetime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Fraction of the lifetime after which clients should renew (0-1).</summary>
    public double RenewalWindow { get; set; } = 0.8;

    /// <summary>Interval to check for on-disk root rotations when RootPfxPath is configured.</summary>
    public TimeSpan RootReloadInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Expected SPIFFE trust domain; used to validate SAN URIs.</summary>
    public string TrustDomain { get; set; } = "spiffe://omnirelay.mesh";

    /// <summary>Require the CSR subject or SAN to bind to the provided node_id.</summary>
    public bool RequireNodeBinding { get; set; } = true;

    /// <summary>Optional path to persist/load the root CA (PFX including private key). If omitted, an in-memory root is generated per process.</summary>
    public string? RootPfxPath { get; set; }

    /// <summary>Password for persisted root PFX (only used when RootPfxPath is specified).</summary>
    public string? RootPfxPassword { get; set; }
}
