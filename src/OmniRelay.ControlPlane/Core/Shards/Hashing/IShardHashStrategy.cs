using Hugo;

namespace OmniRelay.Core.Shards.Hashing;

/// <summary>Strategy for deterministically assigning shards to nodes.</summary>
public interface IShardHashStrategy
{
    string Id { get; }

    Result<ShardHashPlan> Compute(ShardHashRequest request);
}
