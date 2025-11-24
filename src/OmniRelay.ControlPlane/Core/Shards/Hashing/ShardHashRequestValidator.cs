using Hugo;
using static Hugo.Go;

namespace OmniRelay.Core.Shards.Hashing;

internal static class ShardHashRequestValidator
{
    public static Result<ShardHashRequest> Validate(ShardHashRequest? request, string strategyId)
    {
        if (request is null)
        {
            return Err<ShardHashRequest>(ShardHashingErrors.NamespaceRequired(strategyId));
        }

        if (string.IsNullOrWhiteSpace(request.Namespace))
        {
            return Err<ShardHashRequest>(ShardHashingErrors.NamespaceRequired(strategyId));
        }

        if (request.Nodes is null || request.Nodes.Count == 0)
        {
            return Err<ShardHashRequest>(ShardHashingErrors.NodesRequired(strategyId));
        }

        if (!request.Nodes.Any(node => !string.IsNullOrWhiteSpace(node.NodeId)))
        {
            return Err<ShardHashRequest>(ShardHashingErrors.NodeIdInvalid(strategyId));
        }

        if (request.Shards is null)
        {
            return Err<ShardHashRequest>(ShardHashingErrors.ShardsMissing(strategyId));
        }

        return Ok(request);
    }
}
