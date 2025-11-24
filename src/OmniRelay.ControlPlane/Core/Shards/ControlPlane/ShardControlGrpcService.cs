using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Hugo;
using Microsoft.Extensions.Logging;
using MeshControl = OmniRelay.Mesh.Control.V1;

namespace OmniRelay.Core.Shards.ControlPlane;

public sealed class ShardControlGrpcService : MeshControl.ShardControlService.ShardControlServiceBase
{
    private const string MeshScopeHeader = "x-mesh-scope";
    private const string MeshReadScope = "mesh.read";
    private const string MeshOperateScope = "mesh.operate";
    private static readonly char[] MeshScopeSeparators = [' ', ',', ';'];
    private readonly ShardControlPlaneService _service;
    private readonly ILogger<ShardControlGrpcService> _logger;

    public ShardControlGrpcService(
        ShardControlPlaneService service,
        ILogger<ShardControlGrpcService> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<MeshControl.ShardListResponse> List(MeshControl.ShardListRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context, MeshReadScope, MeshOperateScope);
        var filter = CreateFilter(request.Namespace, request.OwnerNodeId, request.Statuses, request.Search);
        var response = await _service.ListAsync(filter, request.Cursor, request.PageSize, context.CancellationToken).ConfigureAwait(false);
        if (response.IsFailure)
        {
            throw MapError(response.Error!);
        }

        var proto = new MeshControl.ShardListResponse
        {
            Version = response.Value.Version,
            NextCursor = response.Value.NextCursor ?? string.Empty
        };
        proto.Shards.AddRange(response.Value.Items.Select(MapSummary));
        return proto;
    }

    public override async Task Watch(
        MeshControl.ShardWatchRequest request,
        IServerStreamWriter<MeshControl.ShardDiffNotification> responseStream,
        ServerCallContext context)
    {
        EnsureAuthorized(context, MeshReadScope, MeshOperateScope);
        var filter = CreateFilter(request.Namespace, request.OwnerNodeId, request.Statuses, request.Search);
        long? resumeToken = request.ResumeToken <= 0 ? null : request.ResumeToken;
        await foreach (var diff in _service.WatchAsync(resumeToken, filter, context.CancellationToken).ConfigureAwait(false))
        {
            if (diff.IsFailure)
            {
                throw MapError(diff.Error!);
            }

            var notification = MapDiff(diff.Value);
            await responseStream.WriteAsync(notification).ConfigureAwait(false);
        }
    }

    public override async Task<MeshControl.ShardDiffResponse> Diff(MeshControl.ShardDiffRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context, MeshOperateScope);
        var filter = CreateFilter(request.Namespace, request.OwnerNodeId, request.Statuses, request.Search);
        var response = await _service.DiffAsync(
            request.FromToken <= 0 ? null : request.FromToken,
            request.ToToken <= 0 ? null : request.ToToken,
            filter,
            context.CancellationToken).ConfigureAwait(false);
        if (response.IsFailure)
        {
            throw MapError(response.Error!);
        }

        var proto = new MeshControl.ShardDiffResponse
        {
            LastPosition = response.Value.LastPosition ?? 0
        };
        proto.Diffs.AddRange(response.Value.Items.Select(entry => new MeshControl.ShardDiffNotification
        {
            Position = entry.Position,
            Current = MapSummary(entry.Current),
            Previous = entry.Previous is null ? null : MapSummary(entry.Previous),
            History = entry.History is null ? null : MapHistory(entry.History)
        }));
        return proto;
    }

    public override async Task<MeshControl.ShardSimulationResponse> Simulate(
        MeshControl.ShardSimulationRequest request,
        ServerCallContext context)
    {
        EnsureAuthorized(context, MeshOperateScope);
        var nodes = request.Nodes
            .Select(node => new ShardSimulationNode(node.NodeId, node.Weight, node.Region, node.Zone))
            .ToArray();
        var simulationRequest = new ShardSimulationRequest
        {
            Namespace = request.Namespace,
            StrategyId = request.StrategyId,
            Nodes = nodes
        };
        var result = await _service.SimulateAsync(simulationRequest, context.CancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            throw MapError(result.Error!);
        }

        var response = new MeshControl.ShardSimulationResponse
        {
            Namespace = result.Value.Namespace,
            StrategyId = result.Value.StrategyId,
            GeneratedAt = Timestamp.FromDateTimeOffset(result.Value.GeneratedAt)
        };
        response.Assignments.AddRange(result.Value.Assignments.Select(MapAssignment));
        response.Changes.AddRange(result.Value.Changes.Select(MapChange));
        return response;
    }

    private static bool HasScope(ServerCallContext context, string requiredScope)
    {
        var header = context.RequestHeaders.Get(MeshScopeHeader);
        if (header is null)
        {
            return false;
        }

        var tokens = header.Value?.Split(MeshScopeSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens?.Any(token => string.Equals(token, requiredScope, StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    private static void EnsureAuthorized(ServerCallContext context, params string[] scopes)
    {
        foreach (var scope in scopes)
        {
            if (HasScope(context, scope))
            {
                return;
            }
        }

        throw new RpcException(new Status(StatusCode.PermissionDenied, "Insufficient scope."));
    }

    private static ShardFilter CreateFilter(
        string? namespaceId,
        string? owner,
        Google.Protobuf.Collections.RepeatedField<MeshControl.ShardStatus> statuses,
        string? search)
    {
        var mapped = statuses.Select(MapStatus).ToArray();
        return new ShardFilter(namespaceId, owner, search, mapped);
    }

    private static MeshControl.ShardRecord MapSummary(ShardSummary summary)
    {
        return new MeshControl.ShardRecord
        {
            Namespace = summary.Namespace,
            ShardId = summary.ShardId,
            StrategyId = summary.StrategyId,
            OwnerNodeId = summary.OwnerNodeId,
            LeaderId = summary.LeaderId ?? string.Empty,
            CapacityHint = summary.CapacityHint,
            Status = MapStatus(summary.Status),
            Version = summary.Version,
            Checksum = summary.Checksum,
            UpdatedAt = Timestamp.FromDateTimeOffset(summary.UpdatedAt),
            ChangeTicket = summary.ChangeTicket ?? string.Empty
        };
    }

    private static MeshControl.ShardHistory MapHistory(ShardHistoryRecord history)
    {
        return new MeshControl.ShardHistory
        {
            Actor = history.Actor,
            Reason = history.Reason,
            ChangeTicket = history.ChangeTicket ?? string.Empty,
            PreviousOwnerNodeId = history.PreviousOwnerNodeId ?? string.Empty
        };
    }

    private static MeshControl.ShardSimulationAssignment MapAssignment(ShardSimulationAssignment assignment)
    {
        return new MeshControl.ShardSimulationAssignment
        {
            Namespace = assignment.Namespace,
            ShardId = assignment.ShardId,
            OwnerNodeId = assignment.OwnerNodeId,
            Capacity = assignment.Capacity,
            LocalityHint = assignment.LocalityHint ?? string.Empty
        };
    }

    private static MeshControl.ShardSimulationChange MapChange(ShardSimulationChange change)
    {
        return new MeshControl.ShardSimulationChange
        {
            Namespace = change.Namespace,
            ShardId = change.ShardId,
            CurrentOwner = change.CurrentOwner,
            ProposedOwner = change.ProposedOwner,
            ChangesOwner = change.ChangesOwner
        };
    }

    private static MeshControl.ShardStatus MapStatus(ShardStatus status)
    {
        return status switch
        {
            ShardStatus.Active => MeshControl.ShardStatus.Active,
            ShardStatus.Draining => MeshControl.ShardStatus.Draining,
            ShardStatus.Paused => MeshControl.ShardStatus.Paused,
            ShardStatus.Disabled => MeshControl.ShardStatus.Disabled,
            _ => MeshControl.ShardStatus.Unspecified
        };
    }

    private static ShardStatus MapStatus(MeshControl.ShardStatus status)
    {
        return status switch
        {
            MeshControl.ShardStatus.Active => ShardStatus.Active,
            MeshControl.ShardStatus.Draining => ShardStatus.Draining,
            MeshControl.ShardStatus.Paused => ShardStatus.Paused,
            MeshControl.ShardStatus.Disabled => ShardStatus.Disabled,
            _ => ShardStatus.Active
        };
    }

    private static RpcException MapError(Error error)
    {
        var statusCode = error.Code switch
        {
            "shards.control.cursor.invalid" => StatusCode.InvalidArgument,
            "shards.control.filter.required" => StatusCode.InvalidArgument,
            "shards.control.namespace.required" => StatusCode.InvalidArgument,
            "shards.control.nodes.required" => StatusCode.InvalidArgument,
            "shards.control.node_id.invalid" => StatusCode.InvalidArgument,
            "shards.hashing.namespace_required" => StatusCode.InvalidArgument,
            "shards.hashing.nodes_required" => StatusCode.InvalidArgument,
            "shards.hashing.node_id_invalid" => StatusCode.InvalidArgument,
            "shards.hashing.strategy.unknown" => StatusCode.InvalidArgument,
            "shards.hashing.shards_missing" => StatusCode.InvalidArgument,
            "shards.control.namespace.missing" => StatusCode.NotFound,
            _ when error.Code is not null && error.Code.Contains("canceled", StringComparison.OrdinalIgnoreCase) => StatusCode.Cancelled,
            _ => StatusCode.Unknown
        };

        var status = new Status(statusCode, error.Message ?? "Shard control-plane error");
        var metadata = new Metadata
        {
            { "error-code", error.Code ?? string.Empty }
        };
        return new RpcException(status, metadata);
    }

    private static MeshControl.ShardDiffNotification MapDiff(ShardRecordDiff diff)
    {
        var current = ShardControlPlaneMapper.ToSummary(diff.Current);
        var previous = diff.Previous is null ? null : ShardControlPlaneMapper.ToSummary(diff.Previous);
        return new MeshControl.ShardDiffNotification
        {
            Position = diff.Position,
            Current = MapSummary(current),
            Previous = previous is null ? null : MapSummary(previous),
            History = diff.History is null ? null : MapHistory(diff.History)
        };
    }
}
