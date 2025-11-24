using Hugo;
using static Hugo.Go;

namespace OmniRelay.Core.Shards.Hashing;

/// <summary>Rendezvous (highest random weight) hashing strategy.</summary>
public sealed class RendezvousShardHashStrategy : IShardHashStrategy
{
    public string Id => ShardHashStrategyIds.Rendezvous;

    public Result<ShardHashPlan> Compute(ShardHashRequest request)
    {
        var validated = ShardHashRequestValidator.Validate(request, Id);
        if (validated.IsFailure)
        {
            return Err<ShardHashPlan>(validated.Error);
        }

        var assignments = new List<ShardAssignment>(request.Shards.Count);
        foreach (var shard in request.Shards)
        {
            var ownerResult = SelectOwner(Id, request.Namespace, shard, request.Nodes);
            if (ownerResult.IsFailure)
            {
                return Err<ShardHashPlan>(ownerResult.Error);
            }

            assignments.Add(new ShardAssignment
            {
                Namespace = request.Namespace,
                ShardId = shard.ShardId,
                OwnerNodeId = ownerResult.Value,
                Capacity = shard.Capacity,
                LocalityHint = shard.LocalityHint
            });
        }

        return Ok(new ShardHashPlan(request.Namespace, Id, assignments, DateTimeOffset.UtcNow));
    }

    internal static Result<string> SelectOwner(
        string strategyId,
        string @namespace,
        ShardDefinition shard,
        IReadOnlyList<ShardNodeDescriptor> nodes)
    {
        string? owner = null;
        double bestScore = double.NegativeInfinity;
        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.NodeId))
            {
                return Err<string>(ShardHashingErrors.NodeIdInvalid(strategyId));
            }

            var hash = ShardHashingPrimitives.Hash($"{@namespace}/{shard.ShardId}::{node.NodeId}");
            var normalized = ShardHashingPrimitives.Normalize(hash);
            var weight = Math.Max(0.001, node.Weight);
            var score = normalized * weight;
            if (score > bestScore)
            {
                bestScore = score;
                owner = node.NodeId;
                continue;
            }

            if (Math.Abs(score - bestScore) < 1e-12 && owner is not null && string.CompareOrdinal(node.NodeId, owner) < 0)
            {
                owner = node.NodeId;
            }
        }

        if (owner is null)
        {
            return Err<string>(ShardHashingErrors.NoEligibleNodes(strategyId));
        }

        return Ok(owner);
    }
}
