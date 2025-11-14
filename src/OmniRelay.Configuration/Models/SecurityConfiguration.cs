using System;
using System.Collections.Generic;

namespace OmniRelay.Configuration.Models;

/// <summary>Security-focused configuration (secrets, enforcement policies, authorization, alerting, bootstrap).</summary>
public sealed class SecurityConfiguration
{
    public SecretsConfiguration Secrets { get; init; } = new();

    public TransportSecurityConfiguration Transport { get; init; } = new();

    public AuthorizationConfiguration Authorization { get; init; } = new();

    public AlertingConfiguration Alerting { get; init; } = new();

    public BootstrapConfiguration Bootstrap { get; init; } = new();
}

/// <summary>Describes secret providers and inline overrides.</summary>
public sealed class SecretsConfiguration
{
    public IList<SecretProviderConfiguration> Providers { get; } = new List<SecretProviderConfiguration>();

    public IDictionary<string, string> Inline { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Secret provider descriptor (environment, file, inline, custom).</summary>
public sealed class SecretProviderConfiguration
{
    public string? Type { get; set; }

    public string? Prefix { get; set; }

    public string? BasePath { get; set; }

    public IDictionary<string, string> Secrets { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Transport security enforcement policy configuration.</summary>
public sealed class TransportSecurityConfiguration
{
    public bool? Enabled { get; set; }

    public IList<string> AllowedProtocols { get; } = new List<string>();

    public IList<string> AllowedTlsVersions { get; } = new List<string>();

    public IList<string> AllowedCipherSuites { get; } = new List<string>();

    public IList<string> AllowedThumbprints { get; } = new List<string>();

    public IList<EndpointPolicyConfiguration> Endpoints { get; } = new List<EndpointPolicyConfiguration>();

    public bool? RequireClientCertificates { get; set; }
}

/// <summary>Endpoint allow/deny rule.</summary>
public sealed class EndpointPolicyConfiguration
{
    public string? Host { get; set; }

    public string? Cidr { get; set; }

    public bool Allow { get; set; } = true;
}

/// <summary>Authorization policy set.</summary>
public sealed class AuthorizationConfiguration
{
    public bool? Enabled { get; set; }

    public IList<AuthorizationPolicyConfiguration> Policies { get; } = new List<AuthorizationPolicyConfiguration>();
}

/// <summary>Individual authorization policy definition.</summary>
public sealed class AuthorizationPolicyConfiguration
{
    public string? Name { get; set; }

    public IList<string> Roles { get; } = new List<string>();

    public IList<string> Clusters { get; } = new List<string>();

    public IDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IList<string> Principals { get; } = new List<string>();

    public bool? RequireMutualTls { get; set; }

    public string? AppliesTo { get; set; }
}

/// <summary>Alerting configuration for diagnostics and incident notifications.</summary>
public sealed class AlertingConfiguration
{
    public bool? Enabled { get; set; }

    public IList<AlertChannelConfiguration> Channels { get; } = new List<AlertChannelConfiguration>();

    public IDictionary<string, AlertTemplateConfiguration> Templates { get; init; } =
        new Dictionary<string, AlertTemplateConfiguration>(StringComparer.OrdinalIgnoreCase);

    public string? DefaultTemplate { get; set; }
}

/// <summary>A notification channel (webhook, Slack, PagerDuty, etc.).</summary>
public sealed class AlertChannelConfiguration
{
    public string? Name { get; set; }

    public string? Type { get; set; }

    public string? Endpoint { get; set; }

    public string? Template { get; set; }

    public string? AuthenticationSecret { get; set; }

    public TimeSpan? Cooldown { get; set; }

    public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Template for alert payloads.</summary>
public sealed class AlertTemplateConfiguration
{
    public string? Title { get; set; }

    public string? Body { get; set; }
}

/// <summary>Bootstrap and join configuration for token/certificate issuance.</summary>
public sealed class BootstrapConfiguration
{
    public bool? Enabled { get; set; }

    public IList<BootstrapTokenConfiguration> Tokens { get; } = new List<BootstrapTokenConfiguration>();

    public string? SeedDirectory { get; set; }
}

/// <summary>Join token definition.</summary>
public sealed class BootstrapTokenConfiguration
{
    public string? Name { get; set; }

    public string? Cluster { get; set; }

    public string? Role { get; set; }

    public TimeSpan? Lifetime { get; set; }

    public int? MaxUses { get; set; }

    public string? Secret { get; set; }
}
