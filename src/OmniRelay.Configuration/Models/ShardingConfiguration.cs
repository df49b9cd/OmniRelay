using OmniRelay.Core.Shards.Hashing;

namespace OmniRelay.Configuration.Models;

/// <summary>Declarative sharding configuration bound from appsettings.</summary>
public sealed class ShardingConfiguration
{
    public IList<ShardNamespaceConfiguration> Namespaces { get; } = [];
}

/// <summary>Configuration for a namespace's shard strategy and node inventory.</summary>
public sealed class ShardNamespaceConfiguration
{
    public const string DefaultStrategy = ShardHashStrategyIds.Rendezvous;

    public string? Namespace { get; set; }

    public string Strategy { get; set; } = DefaultStrategy;

    public IList<ShardNodeConfiguration> Nodes { get; } = [];

    public IList<ShardDefinitionConfiguration> Shards { get; } = [];

    public IDictionary<string, string> Labels { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public double? CapacityHint { get; set; }
}

/// <summary>Configures a node that may own shards.</summary>
public sealed class ShardNodeConfiguration
{
    public string? NodeId { get; set; }

    public double? Weight { get; set; }

    public string? Region { get; set; }

    public string? Zone { get; set; }

    public IDictionary<string, string> Labels { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Configures static shard metadata like locality or capacity hints.</summary>
public sealed class ShardDefinitionConfiguration
{
    public string? ShardId { get; set; }

    public double? Capacity { get; set; }

    public string? LocalityHint { get; set; }
}
