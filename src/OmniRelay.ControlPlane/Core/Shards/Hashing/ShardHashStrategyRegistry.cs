using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Hugo;
using static Hugo.Go;
using Unit = Hugo.Go.Unit;

namespace OmniRelay.Core.Shards.Hashing;

/// <summary>Registry for shard hashing strategies so namespaces can select algorithms by id.</summary>
public sealed class ShardHashStrategyRegistry
{
    private readonly ConcurrentDictionary<string, IShardHashStrategy> _strategies =
        new(StringComparer.OrdinalIgnoreCase);

    public ShardHashStrategyRegistry(IEnumerable<IShardHashStrategy>? strategies = null)
    {
        _ = Register(new RingShardHashStrategy(), overwrite: true);
        _ = Register(new RendezvousShardHashStrategy(), overwrite: true);
        _ = Register(new LocalityAwareShardHashStrategy(), overwrite: true);

        if (strategies is null)
        {
            return;
        }

        foreach (var strategy in strategies)
        {
            _ = Register(strategy, overwrite: true);
        }
    }

    public Result<Unit> Register(IShardHashStrategy strategy, bool overwrite = false)
    {
        if (strategy is null)
        {
            return Err<Unit>(ShardHashingErrors.StrategyRequired());
        }

        if (string.IsNullOrWhiteSpace(strategy.Id))
        {
            return Err<Unit>(ShardHashingErrors.UnknownStrategy(strategy.Id));
        }

        var addResult = _strategies.TryAdd(strategy.Id, strategy);
        if (!addResult && overwrite)
        {
            _strategies[strategy.Id] = strategy;
            return Ok(Unit.Value);
        }

        return addResult
            ? Ok(Unit.Value)
            : Err<Unit>(ShardHashingErrors.DuplicateStrategy(strategy.Id));
    }

    public bool TryGet(string strategyId, [NotNullWhen(true)] out IShardHashStrategy? strategy)
    {
        if (string.IsNullOrWhiteSpace(strategyId))
        {
            strategy = null;
            return false;
        }

        return _strategies.TryGetValue(strategyId, out strategy);
    }

    public Result<IShardHashStrategy> Resolve(string strategyId)
    {
        if (!TryGet(strategyId, out var strategy))
        {
            return Err<IShardHashStrategy>(ShardHashingErrors.UnknownStrategy(strategyId));
        }

        return Ok(strategy);
    }

    public Result<ShardHashPlan> Compute(string strategyId, ShardHashRequest request)
    {
        var strategy = Resolve(strategyId);
        if (strategy.IsFailure)
        {
            return Err<ShardHashPlan>(strategy.Error);
        }

        return strategy.Value.Compute(request);
    }

    public IEnumerable<string> RegisteredStrategyIds => _strategies.Keys;
}
