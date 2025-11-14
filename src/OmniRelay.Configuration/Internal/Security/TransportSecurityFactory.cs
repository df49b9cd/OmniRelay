using System.Collections.Immutable;
using System.Security.Authentication;
using OmniRelay.Configuration.Models;
using OmniRelay.Transport.Security;

namespace OmniRelay.Configuration.Internal.Security;

internal static class TransportSecurityFactory
{
    public static TransportSecurityPolicy? Create(TransportSecurityConfiguration configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        var enabled = configuration.Enabled ?? false;
        if (!enabled)
        {
            return null;
        }

        var protocols = configuration.AllowedProtocols
            .Select(p => p?.Trim().ToLowerInvariant())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        var tlsVersions = configuration.AllowedTlsVersions
            .Select(ParseTlsVersion)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToImmutableHashSet();

        var ciphers = configuration.AllowedCipherSuites
            .Select(ParseCipher)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToImmutableHashSet();

        var thumbprints = configuration.AllowedThumbprints
            .Select(tp => tp?.Trim().ToUpperInvariant())
            .Where(tp => !string.IsNullOrWhiteSpace(tp))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        var rulesBuilder = ImmutableArray.CreateBuilder<TransportEndpointRule>(configuration.Endpoints.Count);
        foreach (var endpoint in configuration.Endpoints)
        {
            IpNetwork? network = null;
            if (!string.IsNullOrWhiteSpace(endpoint.Cidr))
            {
                if (!IpNetwork.TryParse(endpoint.Cidr!, out network))
                {
                    throw new OmniRelayConfigurationException($"Endpoint CIDR '{endpoint.Cidr}' is invalid.");
                }
            }

            var rule = new TransportEndpointRule(endpoint.Allow, endpoint.Host, network);
            rulesBuilder.Add(rule);
        }

        return new TransportSecurityPolicy(
            enabled: true,
            allowedProtocols: protocols,
            allowedTlsVersions: tlsVersions,
            allowedCipherAlgorithms: ciphers,
            requireClientCertificate: configuration.RequireClientCertificates ?? false,
            allowedThumbprints: thumbprints,
            endpointRules: rulesBuilder.ToImmutable());
    }

    private static SslProtocols? ParseTlsVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.ToUpperInvariant() switch
        {
            "TLS1.0" or "TLS" => SslProtocols.Tls,
            "TLS1.1" => SslProtocols.Tls11,
            "TLS1.2" => SslProtocols.Tls12,
            "TLS1.3" => SslProtocols.Tls13,
            _ => throw new OmniRelayConfigurationException($"TLS version '{value}' is not supported.")
        };
    }

    private static CipherAlgorithmType? ParseCipher(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<CipherAlgorithmType>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new OmniRelayConfigurationException($"Cipher algorithm '{value}' is not supported.");
    }
}
