using System.Text;
using Microsoft.Extensions.DependencyInjection;
using OmniRelay.Configuration.Models;
using OmniRelay.Security.Secrets;

namespace OmniRelay.Configuration.Internal.Security;

internal static class SecretProviderFactory
{
    public static ISecretProvider Create(SecretsConfiguration configuration, IServiceProvider services)
    {
        var auditor = services.GetRequiredService<ISecretAccessAuditor>();
        var providers = new List<ISecretProvider>();

        foreach (var descriptor in configuration.Providers)
        {
            providers.Add(CreateProvider(descriptor, auditor));
        }

        if (configuration.Inline.Count > 0)
        {
            providers.Add(CreateInlineProvider(configuration.Inline, auditor));
        }

        if (providers.Count == 0)
        {
            providers.Add(new EnvironmentSecretProvider(auditor));
        }

        return new CompositeSecretProvider(providers, auditor);
    }

    private static ISecretProvider CreateProvider(SecretProviderConfiguration descriptor, ISecretAccessAuditor auditor)
    {
        var type = descriptor.Type?.Trim();
        return type?.ToLowerInvariant() switch
        {
            null or "" or "environment" or "env" => new EnvironmentSecretProvider(auditor, descriptor.Prefix),
            "file" => CreateFileProvider(descriptor, auditor),
            "inline" => CreateInlineProvider(descriptor.Secrets, auditor),
            _ => throw new OmniRelayConfigurationException($"Unknown secret provider type '{descriptor.Type}'. Supported types: environment, file, inline.")
        };
    }

    private static FileSecretProvider CreateFileProvider(SecretProviderConfiguration descriptor, ISecretAccessAuditor auditor)
    {
        if (descriptor.Secrets.Count == 0)
        {
            throw new OmniRelayConfigurationException("File secret providers must specify at least one secret mapping.");
        }

        var options = new FileSecretProviderOptions
        {
            BaseDirectory = string.IsNullOrWhiteSpace(descriptor.BasePath)
                ? AppContext.BaseDirectory
                : descriptor.BasePath!
        };

        foreach (var (name, path) in descriptor.Secrets)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            options.Secrets[name] = path;
        }

        return new FileSecretProvider(options, auditor);
    }

    private static InMemorySecretProvider CreateInlineProvider(IDictionary<string, string> values, ISecretAccessAuditor auditor)
    {
        var provider = new InMemorySecretProvider(auditor);
        foreach (var (name, value) in values)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var buffer = Encoding.UTF8.GetBytes(value ?? string.Empty);
            provider.SetSecret(name, buffer);
        }

        return provider;
    }
}
