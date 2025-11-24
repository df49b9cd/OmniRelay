using Hugo;

namespace OmniRelay.Core.Shards.Hashing;

/// <summary>Shared error factories for shard hashing inputs and registry operations.</summary>
internal static class ShardHashingErrors
{
    private const string NamespaceRequiredCode = "shards.hashing.namespace_required";
    private const string NodesRequiredCode = "shards.hashing.nodes_required";
    private const string NodeIdInvalidCode = "shards.hashing.node_id_invalid";
    private const string ShardsMissingCode = "shards.hashing.shards_missing";
    private const string NoEligibleNodesCode = "shards.hashing.no_eligible_nodes";
    private const string UnknownStrategyCode = "shards.hashing.strategy.unknown";
    private const string DuplicateStrategyCode = "shards.hashing.strategy.duplicate";
    private const string StrategyRequiredCode = "shards.hashing.strategy.required";

    public static Error NamespaceRequired(string strategyId) =>
        Error.From("Shard namespace is required to compute assignments.", NamespaceRequiredCode)
            .WithMetadata("strategyId", strategyId);

    public static Error NodesRequired(string strategyId) =>
        Error.From("At least one node is required to compute shard assignments.", NodesRequiredCode)
            .WithMetadata("strategyId", strategyId);

    public static Error NodeIdInvalid(string strategyId) =>
        Error.From("Shard nodes must supply a non-empty nodeId.", NodeIdInvalidCode)
            .WithMetadata("strategyId", strategyId);

    public static Error ShardsMissing(string strategyId) =>
        Error.From("Shard definitions are required to compute assignments.", ShardsMissingCode)
            .WithMetadata("strategyId", strategyId);

    public static Error NoEligibleNodes(string strategyId) =>
        Error.From("No eligible nodes were available for shard hashing.", NoEligibleNodesCode)
            .WithMetadata("strategyId", strategyId);

    public static Error UnknownStrategy(string? strategyId) =>
        Error.From("Requested shard hash strategy is not registered.", UnknownStrategyCode)
            .WithMetadata("strategyId", strategyId ?? string.Empty);

    public static Error DuplicateStrategy(string strategyId) =>
        Error.From("Shard hash strategy id is already registered.", DuplicateStrategyCode)
            .WithMetadata("strategyId", strategyId);

    public static Error StrategyRequired() =>
        Error.From("Shard hash strategy cannot be null.", StrategyRequiredCode);
}
