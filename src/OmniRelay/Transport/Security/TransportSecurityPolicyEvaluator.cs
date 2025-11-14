using System.Net;
using Microsoft.Extensions.Logging;

namespace OmniRelay.Transport.Security;

/// <summary>Evaluates requests against the configured transport security policy.</summary>
public sealed class TransportSecurityPolicyEvaluator
{
    private readonly TransportSecurityPolicy _policy;
    private readonly ILogger<TransportSecurityPolicyEvaluator> _logger;

    public TransportSecurityPolicyEvaluator(TransportSecurityPolicy policy, ILogger<TransportSecurityPolicyEvaluator> logger)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public TransportSecurityDecision Evaluate(TransportSecurityContext context)
    {
        if (!_policy.Enabled)
        {
            return TransportSecurityDecision.Allowed;
        }

        var normalizedProtocol = context.Protocol?.ToLowerInvariant() ?? string.Empty;
        if (_policy.AllowedProtocols.Count > 0 && !_policy.AllowedProtocols.Contains(normalizedProtocol))
        {
            var reason = $"Protocol '{context.Protocol}' is not allowed.";
            _logger.LogWarning("Transport security denied connection: {Reason}", reason);
            return new TransportSecurityDecision(false, reason);
        }

        if (_policy.AllowedTlsVersions.Count > 0)
        {
            if (context.TlsProtocol is not { } tls || !_policy.AllowedTlsVersions.Contains(tls))
            {
                var reason = "TLS protocol mismatch.";
                _logger.LogWarning("Transport security denied connection: {Reason}", reason);
                return new TransportSecurityDecision(false, reason);
            }
        }

        if (_policy.AllowedCipherAlgorithms.Count > 0)
        {
            if (context.Cipher is not { } cipher || !_policy.AllowedCipherAlgorithms.Contains(cipher))
            {
                var reason = "Cipher suite not permitted.";
                _logger.LogWarning("Transport security denied connection: {Reason}", reason);
                return new TransportSecurityDecision(false, reason);
            }
        }

        if (_policy.RequireClientCertificate && context.ClientCertificate is null)
        {
            var reason = "Client certificate required.";
            _logger.LogWarning("Transport security denied connection: {Reason}", reason);
            return new TransportSecurityDecision(false, reason);
        }

        if (_policy.AllowedThumbprints.Count > 0 && context.ClientCertificate is not null)
        {
            var thumbprint = context.ClientCertificate.Thumbprint?.ToUpperInvariant();
            if (thumbprint is null || !_policy.AllowedThumbprints.Contains(thumbprint))
            {
                var reason = "Client certificate thumbprint not allowed.";
                _logger.LogWarning("Transport security denied connection: {Reason}", reason);
                return new TransportSecurityDecision(false, reason);
            }
        }

        if (_policy.EndpointRules.Length > 0)
        {
            var decision = EvaluateEndpoints(context);
            if (!decision.IsAllowed)
            {
                _logger.LogWarning("Transport security denied connection: {Reason}", decision.Reason);
                return decision;
            }
        }

        return TransportSecurityDecision.Allowed;
    }

    private TransportSecurityDecision EvaluateEndpoints(TransportSecurityContext context)
    {
        IPAddress? address = context.RemoteAddress;
        var host = context.Host;

        foreach (var rule in _policy.EndpointRules)
        {
            if (rule.Matches(address, host))
            {
                if (rule.Allow)
                {
                    return TransportSecurityDecision.Allowed;
                }

                var reason = "Endpoint blocked by policy.";
                return new TransportSecurityDecision(false, reason);
            }
        }

        // Default deny when rules exist but nothing matched.
        if (_policy.EndpointRules.Length > 0)
        {
            return new TransportSecurityDecision(false, "No endpoint rules matched.");
        }

        return TransportSecurityDecision.Allowed;
    }
}
