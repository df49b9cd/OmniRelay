using Microsoft.Extensions.DependencyInjection;
using OmniRelay.ControlPlane.Bootstrap;
using OmniRelay.ControlPlane.Identity;
using OmniRelay.ControlPlane.Agent;
using OmniRelay.ControlPlane.Security;

namespace OmniRelay.Plugins.Internal.Identity;

public static class IdentityPluginServiceCollectionExtensions
{
    public static IServiceCollection AddInternalIdentityPlugins(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<CertificateAuthorityService>();
        services.AddSingleton<SpiffeWorkloadIdentityProvider>();
        services.AddSingleton<FileBootstrapIdentityProvider>();
        services.AddSingleton<TransportTlsManager>();
        services.AddSingleton<AgentCertificateManager>();
        return services;
    }
}
