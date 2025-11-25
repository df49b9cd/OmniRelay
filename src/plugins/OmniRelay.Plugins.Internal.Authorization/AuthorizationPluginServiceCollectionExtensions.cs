using Microsoft.Extensions.DependencyInjection;
using OmniRelay.ControlPlane.Bootstrap;
using OmniRelay.DataPlane.Security.Authorization;
using OmniRelay.DataPlane.Transport.Security;
using OmniRelay.Dispatcher.Config;

namespace OmniRelay.Plugins.Internal.Authorization;

public static class AuthorizationPluginServiceCollectionExtensions
{
    public static IServiceCollection AddInternalAuthorizationPlugins(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MeshAuthorizationEvaluator>();
        services.AddSingleton<TransportSecurityPolicyEvaluator>();
        services.AddSingleton<BootstrapPolicyEvaluator>();
        services.AddSingleton<TransportPolicyEvaluator>();
        return services;
    }
}
