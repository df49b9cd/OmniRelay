using OmniRelay.Configuration.Models;
using OmniRelay.Core.Shards.Hashing;

namespace OmniRelay.Configuration.Sharding;

/// <summary>Helpers that translate configuration models into shard hashing requests.</summary>
public static class ShardingConfigurationExtensions
{
    public static ShardHashRequest ToHashRequest(this ShardNamespaceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var namespaceId = configuration.Namespace;
        if (string.IsNullOrWhiteSpace(namespaceId))
        {
            throw new OmniRelayConfigurationException("Shard namespace configuration must specify a namespace id.");
        }

        if (configuration.Nodes.Count == 0)
        {
            throw new OmniRelayConfigurationException($"Shard namespace '{namespaceId}' must declare at least one node.");
        }

        if (configuration.Shards.Count == 0)
        {
            throw new OmniRelayConfigurationException($"Shard namespace '{namespaceId}' must include at least one shard definition.");
        }

        var nodes = configuration.Nodes
            .Select(node => new ShardNodeDescriptor
            {
                NodeId = node.NodeId ?? throw new OmniRelayConfigurationException($"Shard namespace '{namespaceId}' includes a node without an id."),
                Weight = node.Weight is { } weight and > 0 ? weight : 1,
                Region = node.Region,
                Zone = node.Zone,
                Labels = new Dictionary<string, string>(node.Labels, StringComparer.OrdinalIgnoreCase)
            })
            .ToArray();

        var shardCapacity = configuration.CapacityHint.GetValueOrDefault(1);
        var shards = configuration.Shards
            .Select(shard => new ShardDefinition
            {
                ShardId = shard.ShardId ?? throw new OmniRelayConfigurationException($"Shard namespace '{namespaceId}' includes a shard without an id."),
                Capacity = shard.Capacity.GetValueOrDefault(shardCapacity),
                LocalityHint = shard.LocalityHint,
                Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            })
            .ToArray();

        return new ShardHashRequest
        {
            Namespace = namespaceId,
            Nodes = nodes,
            Shards = shards
        };
    }

    public static ShardHashPlan ComputePlan(this ShardNamespaceConfiguration configuration, ShardHashStrategyRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(registry);
        var request = configuration.ToHashRequest();
        var strategy = string.IsNullOrWhiteSpace(configuration.Strategy)
            ? ShardHashStrategyIds.Rendezvous
            : configuration.Strategy;
        return registry.Compute(strategy, request);
    }
}
