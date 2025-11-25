using Microsoft.Extensions.DependencyInjection;
using OmniRelay.Security.Authorization;
using OmniRelay.Transport.Security;

namespace OmniRelay.Plugins.Internal.Transport;

/// <summary>Reusable DI defaults for transport security and authorization.</summary>
internal static class ServiceCollectionTransportDefaults
{
    public static IServiceCollection AddTransportSecurityDefaults(this IServiceCollection services)
    {
        services.AddSingleton<TransportSecurityPolicyEvaluator>();
        services.AddSingleton<MeshAuthorizationEvaluator>();
        return services;
    }
}
