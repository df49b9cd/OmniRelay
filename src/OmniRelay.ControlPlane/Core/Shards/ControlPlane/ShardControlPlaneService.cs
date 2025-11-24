using Hugo;
using Hugo.Policies;
using Microsoft.Extensions.Logging;
using OmniRelay.Core.Shards.Hashing;
using static Hugo.Go;

namespace OmniRelay.Core.Shards.ControlPlane;

public sealed partial class ShardControlPlaneService
{
    private static readonly ResultExecutionPolicy DefaultRepositoryPolicy =
        ResultExecutionPolicy.None.WithRetry(ResultRetryPolicy.Exponential(
            3,
            TimeSpan.FromMilliseconds(25),
            2.0,
            TimeSpan.FromMilliseconds(500)));

    private readonly IShardRepository _repository;
    private readonly ShardHashStrategyRegistry _strategies;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ShardControlPlaneService> _logger;
    private readonly ResultExecutionPolicy _repositoryPolicy;

    public ShardControlPlaneService(
        IShardRepository repository,
        ShardHashStrategyRegistry strategies,
        TimeProvider? timeProvider,
        ILogger<ShardControlPlaneService> logger,
        ResultExecutionPolicy? repositoryPolicy = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryPolicy = repositoryPolicy ?? DefaultRepositoryPolicy;
    }

    public async ValueTask<Result<ShardListResponse>> ListAsync(
        ShardFilter filter,
        string? cursorToken,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        if (filter is null)
        {
            return Err<ShardListResponse>(ShardControlPlaneErrors.FilterRequired());
        }

        ShardQueryCursor? cursor = null;

        if (!string.IsNullOrWhiteSpace(cursorToken) &&
            !ShardQueryCursor.TryParse(cursorToken, out cursor))
        {
            return Err<ShardListResponse>(ShardControlPlaneErrors.InvalidCursor(cursorToken));
        }

        var options = filter.ToQueryOptions(pageSize, cursor);
        return await Result.RetryWithPolicyAsync<ShardListResponse>(
            async (_, token) =>
            {
                var result = await _repository.QueryAsync(options, token).ConfigureAwait(false);
                var items = result.Items.Select(ShardControlPlaneMapper.ToSummary).ToArray();
                return Ok(new ShardListResponse(items, result.NextCursor?.Encode(), result.HighestVersion));
            },
            _repositoryPolicy,
            _timeProvider,
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Result<ShardDiffResponse>> DiffAsync(
        long? fromPosition,
        long? toPosition,
        ShardFilter filter,
        CancellationToken cancellationToken)
    {
        if (filter is null)
        {
            return Err<ShardDiffResponse>(ShardControlPlaneErrors.FilterRequired());
        }

        var since = fromPosition.HasValue ? Math.Max(0, fromPosition.Value - 1) : (long?)null;
        var upperBound = toPosition ?? long.MaxValue;
        var diffs = new List<ShardDiffEntry>();

        try
        {
            await foreach (var diff in _repository.StreamDiffsAsync(since, cancellationToken).ConfigureAwait(false))
            {
                if (fromPosition.HasValue && diff.Position < fromPosition.Value)
                {
                    continue;
                }

                if (diff.Position > upperBound)
                {
                    break;
                }

                if (!filter.Matches(diff.Current))
                {
                    continue;
                }

                diffs.Add(new ShardDiffEntry(
                    diff.Position,
                    ShardControlPlaneMapper.ToSummary(diff.Current),
                    diff.Previous is null ? null : ShardControlPlaneMapper.ToSummary(diff.Previous),
                    diff.History));
            }
        }
        catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
        {
            return Err<ShardDiffResponse>(Error.Canceled("Shard diff canceled.", cancellationToken)
                .WithMetadata("stage", "shards.diff"));
        }
        catch (Exception ex)
        {
            Log.DiffStreamFailed(_logger, ex);
            return Err<ShardDiffResponse>(ShardControlPlaneErrors.StreamFailure(ex, "shards.diff"));
        }

        var last = diffs.Count > 0 ? diffs[^1].Position : (long?)null;
        return Ok(new ShardDiffResponse(diffs, last));
    }

    public IAsyncEnumerable<Result<ShardRecordDiff>> WatchAsync(
        long? resumeToken,
        ShardFilter filter,
        CancellationToken cancellationToken)
    {
        if (filter is null)
        {
            return AsyncEnumerable.Repeat(
                Err<ShardRecordDiff>(ShardControlPlaneErrors.FilterRequired()),
                1);
        }

        var stream = Result.MapStreamAsync<ShardRecordDiff, ShardRecordDiff>(
            _repository.StreamDiffsAsync(resumeToken, cancellationToken),
            (diff, _) => new ValueTask<Result<ShardRecordDiff>>(Ok(diff)),
            cancellationToken);

        return Result.FilterStreamAsync(
            stream,
            diff => filter.Matches(diff.Current),
            cancellationToken);
    }

    public async ValueTask<Result<ShardSimulationResponse>> SimulateAsync(
        ShardSimulationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Err<ShardSimulationResponse>(ShardControlPlaneErrors.SimulationRequestRequired());
        }

        if (string.IsNullOrWhiteSpace(request.Namespace))
        {
            return Err<ShardSimulationResponse>(ShardControlPlaneErrors.NamespaceRequired());
        }

        if (request.Nodes is null || request.Nodes.Count == 0)
        {
            return Err<ShardSimulationResponse>(ShardControlPlaneErrors.NodesRequired());
        }

        var resolvedStrategy = string.IsNullOrWhiteSpace(request.StrategyId)
            ? ShardHashStrategyIds.Rendezvous
            : request.StrategyId!;

        var nodeDescriptors = new List<ShardNodeDescriptor>(request.Nodes.Count);
        foreach (var node in request.Nodes)
        {
            if (node is null || string.IsNullOrWhiteSpace(node.NodeId))
            {
                return Err<ShardSimulationResponse>(ShardControlPlaneErrors.NodeIdInvalid(node?.NodeId));
            }

            nodeDescriptors.Add(new ShardNodeDescriptor
            {
                NodeId = node.NodeId,
                Weight = node.Weight.GetValueOrDefault(1),
                Region = node.Region,
                Zone = node.Zone
            });
        }

        var existingResult = await Result.RetryWithPolicyAsync<IReadOnlyList<ShardRecord>>(
            async (_, token) =>
            {
                var records = await _repository.ListAsync(request.Namespace, token).ConfigureAwait(false);
                return Ok((IReadOnlyList<ShardRecord>)records);
            },
            _repositoryPolicy,
            _timeProvider,
            cancellationToken).ConfigureAwait(false);

        if (existingResult.IsFailure)
        {
            return Err<ShardSimulationResponse>(existingResult.Error);
        }

        var existing = existingResult.Value;
        if (existing.Count == 0)
        {
            Log.SimulationNamespaceMissing(_logger, request.Namespace);
            return Err<ShardSimulationResponse>(ShardControlPlaneErrors.NamespaceMissing(request.Namespace));
        }

        var definitions = existing.Select(record => new ShardDefinition
        {
            ShardId = record.ShardId,
            Capacity = record.CapacityHint
        }).ToArray();

        var hashRequest = new ShardHashRequest
        {
            Namespace = request.Namespace,
            Nodes = nodeDescriptors,
            Shards = definitions
        };

        var plan = _strategies.Compute(resolvedStrategy, hashRequest);
        if (plan.IsFailure)
        {
            return Err<ShardSimulationResponse>(plan.Error);
        }

        var assignments = plan.Value.Assignments.Select(ShardControlPlaneMapper.ToAssignment).ToArray();
        var lookup = existing.ToDictionary(r => r.ShardId, StringComparer.OrdinalIgnoreCase);

        var changes = plan.Value.Assignments
            .Where(assignment => lookup.TryGetValue(assignment.ShardId, out var record) &&
                                 !string.Equals(record!.OwnerNodeId, assignment.OwnerNodeId, StringComparison.Ordinal))
            .Select(assignment => ShardControlPlaneMapper.ToChange(assignment, lookup[assignment.ShardId]))
            .ToArray();

        return Ok(new ShardSimulationResponse(
            request.Namespace,
            resolvedStrategy,
            _timeProvider.GetUtcNow(),
            assignments,
            changes));
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Shard diff stream failed.")]
        public static partial void DiffStreamFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Shard simulation requested for namespace {Namespace} but no shard records exist.")]
        public static partial void SimulationNamespaceMissing(ILogger logger, string @namespace);
    }
}
