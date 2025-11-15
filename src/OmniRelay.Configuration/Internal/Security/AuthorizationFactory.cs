using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using OmniRelay.Configuration.Models;
using OmniRelay.Security.Authorization;

namespace OmniRelay.Configuration.Internal.Security;

internal static partial class AuthorizationFactory
{
    public static MeshAuthorizationEvaluator? Create(AuthorizationConfiguration configuration, ILogger logger)
    {
        if (configuration is null || configuration.Enabled != true || configuration.Policies.Count == 0)
        {
            return null;
        }

        var policies = new List<MeshAuthorizationPolicy>();
        foreach (var policyConfig in configuration.Policies)
        {
            if (string.IsNullOrWhiteSpace(policyConfig.Name))
            {
                continue;
            }

            var transport = "*";
            string? pathPrefix = null;
            if (!string.IsNullOrWhiteSpace(policyConfig.AppliesTo))
            {
                var parts = policyConfig.AppliesTo.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    transport = parts[0];
                    pathPrefix = parts[1];
                }
                else if (parts.Length == 1)
                {
                    pathPrefix = parts[0];
                }
            }

            var roles = policyConfig.Roles.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            var clusters = policyConfig.Clusters.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            var labels = policyConfig.Labels.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
            var principals = policyConfig.Principals.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

            var policy = new MeshAuthorizationPolicy(
                policyConfig.Name!,
                transport,
                pathPrefix,
                roles,
                clusters,
                labels,
                principals,
                policyConfig.RequireMutualTls ?? false);
            policies.Add(policy);
        }

        if (policies.Count == 0)
        {
            Log.AuthorizationMissingPolicies(logger);
            return null;
        }

        return new MeshAuthorizationEvaluator(policies);
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Authorization was enabled but no valid policies were configured.")]
        public static partial void AuthorizationMissingPolicies(ILogger logger);
    }
}
