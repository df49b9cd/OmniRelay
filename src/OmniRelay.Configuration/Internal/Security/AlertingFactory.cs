using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniRelay.Configuration.Models;
using OmniRelay.Diagnostics.Alerting;
using OmniRelay.Security.Secrets;

namespace OmniRelay.Configuration.Internal.Security;

internal static class AlertingFactory
{
    public static IAlertPublisher? Create(AlertingConfiguration configuration, IServiceProvider services)
    {
        if (configuration is null || configuration.Enabled != true)
        {
            return null;
        }

        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var logger = services.GetRequiredService<ILogger<AlertPublisher>>();
        var secretProvider = services.GetService<ISecretProvider>();
        var channels = new List<IAlertChannel>();
        var cooldowns = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);

        foreach (var channel in configuration.Channels)
        {
            if (channel is null || string.IsNullOrWhiteSpace(channel.Name))
            {
                continue;
            }

            if (!Uri.TryCreate(channel.Endpoint, UriKind.Absolute, out var endpoint))
            {
                throw new OmniRelayConfigurationException($"Alert channel '{channel.Name}' endpoint '{channel.Endpoint}' is not a valid absolute URI.");
            }

            string? authToken = channel.AuthenticationSecret;
            if (!string.IsNullOrWhiteSpace(authToken) && secretProvider is not null)
            {
                using var secret = secretProvider.GetSecretAsync(authToken).GetAwaiter().GetResult();
                authToken = secret?.AsString();
            }

            var headers = channel.Headers?.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value is not null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var httpClient = httpClientFactory.CreateClient($"alert-{channel.Name}");
            var webhook = new WebhookAlertChannel(channel.Name, endpoint, httpClient, headers, authToken);
            channels.Add(webhook);

            if (channel.Cooldown is { } cooldown)
            {
                cooldowns[channel.Name] = cooldown;
            }
        }

        if (channels.Count == 0)
        {
            return null;
        }

        var defaultCooldown = TimeSpan.FromMinutes(1);
        return new AlertPublisher(channels, cooldowns, defaultCooldown, logger);
    }
}
