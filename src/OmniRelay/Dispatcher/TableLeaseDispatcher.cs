using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hugo;
using OmniRelay.Core;
using OmniRelay.Errors;
using static Hugo.Go;

namespace OmniRelay.Dispatcher;

/// <summary>
/// Hosts a SafeTaskQueue-backed table lease queue and exposes canonical procedures for enqueue, lease, ack, and drain flows.
/// </summary>
public sealed class TableLeaseDispatcherComponent : IAsyncDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly TableLeaseDispatcherOptions _options;
    private readonly TaskQueue<TableLeaseWorkItem> _queue;
    private readonly SafeTaskQueueWrapper<TableLeaseWorkItem> _safeQueue;
    private readonly ConcurrentDictionary<TaskQueueOwnershipToken, SafeTaskQueueLease<TableLeaseWorkItem>> _leases = new();
    private readonly string _enqueueProcedure;
    private readonly string _leaseProcedure;
    private readonly string _completeProcedure;
    private readonly string _heartbeatProcedure;
    private readonly string _failProcedure;
    private readonly string _drainProcedure;
    private readonly string _restoreProcedure;

    /// <summary>
    /// Creates a table lease component and immediately registers the standard procedures on the supplied dispatcher.
    /// </summary>
    public TableLeaseDispatcherComponent(Dispatcher dispatcher, TableLeaseDispatcherOptions options)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        var queueOptions = _options.QueueOptions ?? throw new ArgumentException("Queue options are required.", nameof(options));

        var prefix = string.IsNullOrWhiteSpace(_options.Namespace)
            ? "tablelease"
            : _options.Namespace.Trim();

        _enqueueProcedure = $"{prefix}::enqueue";
        _leaseProcedure = $"{prefix}::lease";
        _completeProcedure = $"{prefix}::complete";
        _heartbeatProcedure = $"{prefix}::heartbeat";
        _failProcedure = $"{prefix}::fail";
        _drainProcedure = $"{prefix}::drain";
        _restoreProcedure = $"{prefix}::restore";

        _queue = new TaskQueue<TableLeaseWorkItem>(queueOptions);
        _safeQueue = new SafeTaskQueueWrapper<TableLeaseWorkItem>(_queue, ownsQueue: false);

        RegisterProcedures();
    }

    private void RegisterProcedures()
    {
        _dispatcher.RegisterJsonUnary<TableLeaseEnqueueRequest, TableLeaseEnqueueResponse>(
            _enqueueProcedure,
            HandleEnqueue);

        _dispatcher.RegisterJsonUnary<TableLeaseLeaseRequest, TableLeaseLeaseResponse>(
            _leaseProcedure,
            HandleLease);

        _dispatcher.RegisterJsonUnary<TableLeaseCompleteRequest, TableLeaseAcknowledgeResponse>(
            _completeProcedure,
            HandleComplete);

        _dispatcher.RegisterJsonUnary<TableLeaseHeartbeatRequest, TableLeaseAcknowledgeResponse>(
            _heartbeatProcedure,
            HandleHeartbeat);

        _dispatcher.RegisterJsonUnary<TableLeaseFailRequest, TableLeaseAcknowledgeResponse>(
            _failProcedure,
            HandleFail);

        _dispatcher.RegisterJsonUnary<TableLeaseDrainRequest, TableLeaseDrainResponse>(
            _drainProcedure,
            HandleDrain);

        _dispatcher.RegisterJsonUnary<TableLeaseRestoreRequest, TableLeaseRestoreResponse>(
            _restoreProcedure,
            HandleRestore);
    }

    private async ValueTask<TableLeaseEnqueueResponse> HandleEnqueue(JsonUnaryContext context, TableLeaseEnqueueRequest request)
    {
        if (request.Payload is null)
        {
            throw new ResultException(Error.From("table lease payload is required", "error.tablelease.payload_missing", cause: null!, metadata: null));
        }

        var workItem = TableLeaseWorkItem.FromPayload(request.Payload);
        var enqueue = await _safeQueue.EnqueueAsync(workItem, context.CancellationToken).ConfigureAwait(false);
        enqueue.ThrowIfFailure();

        return new TableLeaseEnqueueResponse(GetStats());
    }

    private async ValueTask<TableLeaseLeaseResponse> HandleLease(JsonUnaryContext context, TableLeaseLeaseRequest request)
    {
        var leaseResult = await _safeQueue.LeaseAsync(context.CancellationToken).ConfigureAwait(false);
        var lease = leaseResult.ValueOrThrow();

        if (!_leases.TryAdd(lease.OwnershipToken, lease))
        {
            // Extremely unlikely: duplicate token. Fail the lease so it is re-queued.
            await lease.FailAsync(
                Error.From("duplicate ownership token detected", "error.tablelease.duplicate_token", cause: null!, metadata: null),
                requeue: true,
                context.CancellationToken).ConfigureAwait(false);

            throw new ResultException(Error.From("duplicate ownership token detected", "error.tablelease.duplicate_token", cause: null!, metadata: null));
        }

        return TableLeaseLeaseResponse.FromLease(lease);
    }

    private async ValueTask<TableLeaseAcknowledgeResponse> HandleComplete(JsonUnaryContext context, TableLeaseCompleteRequest request)
    {
        if (!TryGetLease(request.OwnershipToken, out var lease))
        {
            return TableLeaseAcknowledgeResponse.NotFound("error.tablelease.unknown_token", "Lease token was not found.");
        }

        var complete = await lease.CompleteAsync(context.CancellationToken).ConfigureAwait(false);
        if (complete.IsFailure)
        {
            return TableLeaseAcknowledgeResponse.FromError(complete.Error!);
        }

        CleanupLease(request.OwnershipToken);
        return TableLeaseAcknowledgeResponse.Ack();
    }

    private async ValueTask<TableLeaseAcknowledgeResponse> HandleHeartbeat(JsonUnaryContext context, TableLeaseHeartbeatRequest request)
    {
        if (!TryGetLease(request.OwnershipToken, out var lease))
        {
            return TableLeaseAcknowledgeResponse.NotFound("error.tablelease.unknown_token", "Lease token was not found.");
        }

        var heartbeat = await lease.HeartbeatAsync(context.CancellationToken).ConfigureAwait(false);
        return heartbeat.IsSuccess
            ? TableLeaseAcknowledgeResponse.Ack()
            : TableLeaseAcknowledgeResponse.FromError(heartbeat.Error!);
    }

    private async ValueTask<TableLeaseAcknowledgeResponse> HandleFail(JsonUnaryContext context, TableLeaseFailRequest request)
    {
        if (!TryGetLease(request.OwnershipToken, out var lease))
        {
            return TableLeaseAcknowledgeResponse.NotFound("error.tablelease.unknown_token", "Lease token was not found.");
        }

        var error = request.ToError();
        var fail = await lease.FailAsync(error, request.Requeue, context.CancellationToken).ConfigureAwait(false);
        if (fail.IsFailure)
        {
            return TableLeaseAcknowledgeResponse.FromError(fail.Error!);
        }

        CleanupLease(request.OwnershipToken);
        return TableLeaseAcknowledgeResponse.Ack();
    }

    private async ValueTask<TableLeaseDrainResponse> HandleDrain(JsonUnaryContext context, TableLeaseDrainRequest request)
    {
        var drained = await _queue.DrainPendingItemsAsync(context.CancellationToken).ConfigureAwait(false);
        var payloads = drained
            .Select(TableLeasePendingItemDto.FromPending)
            .ToImmutableArray();

        // Drain removes the work from the queue; reset stats so operators can see the impact.
        return new TableLeaseDrainResponse(payloads);
    }

    private async ValueTask<TableLeaseRestoreResponse> HandleRestore(JsonUnaryContext context, TableLeaseRestoreRequest request)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            return new TableLeaseRestoreResponse(0);
        }

        var pending = request.Items
            .Select(item => item.ToPending())
            .ToList();

        await _queue.RestorePendingItemsAsync(pending, context.CancellationToken).ConfigureAwait(false);
        return new TableLeaseRestoreResponse(pending.Count);
    }

    private TableLeaseQueueStats GetStats() =>
        new(_queue.PendingCount, _queue.ActiveLeaseCount);

    private bool TryGetLease(TableLeaseOwnershipHandle handle, out SafeTaskQueueLease<TableLeaseWorkItem> lease)
    {
        var token = handle.ToToken();
        return _leases.TryGetValue(token, out lease);
    }

    private void CleanupLease(TableLeaseOwnershipHandle? handle)
    {
        if (handle is null)
        {
            return;
        }

        var token = handle!.ToToken();
        _leases.TryRemove(token, out _);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var lease in _leases.Values)
        {
            try
            {
                await lease.FailAsync(
                    Error.Canceled("lease disposed", token: null),
                    requeue: true,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Ignored: we are disposing and best-effort requeueing outstanding work.
            }
        }

        _leases.Clear();
        await _safeQueue.DisposeAsync().ConfigureAwait(false);
        await _queue.DisposeAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Options used by <see cref="TableLeaseDispatcherComponent"/>.
/// </summary>
public sealed class TableLeaseDispatcherOptions
{
    /// <summary>The namespace prefix applied to the registered procedures. Defaults to 'tablelease'.</summary>
    public string Namespace { get; init; } = "tablelease";

    /// <summary>Task queue options that control capacity, lease duration, and heartbeat cadence.</summary>
    public TaskQueueOptions? QueueOptions { get; init; } = new();
}

/// <summary>Represents the serialized form of a work item stored in the queue.</summary>
public sealed record TableLeaseItemPayload(
    string Namespace,
    string Table,
    string PartitionKey,
    string PayloadEncoding,
    byte[] Body,
    IReadOnlyDictionary<string, string>? Attributes = null,
    string? RequestId = null);

public sealed record TableLeaseEnqueueRequest(TableLeaseItemPayload? Payload);

public sealed record TableLeaseEnqueueResponse(TableLeaseQueueStats Stats);

public sealed record TableLeaseLeaseRequest;

public sealed record TableLeaseLeaseResponse(
    TableLeaseItemPayload Payload,
    long SequenceId,
    int Attempt,
    DateTimeOffset EnqueuedAt,
    TableLeaseErrorInfo? LastError,
    TableLeaseOwnershipHandle OwnershipToken)
{
    internal static TableLeaseLeaseResponse FromLease(SafeTaskQueueLease<TableLeaseWorkItem> lease)
    {
        var payload = lease.Value.ToPayload();
        return new TableLeaseLeaseResponse(
            payload,
            lease.SequenceId,
            lease.Attempt,
            lease.EnqueuedAt,
            TableLeaseErrorInfo.FromError(lease.LastError),
            TableLeaseOwnershipHandle.FromToken(lease.OwnershipToken));
    }
}

public sealed record TableLeaseCompleteRequest(TableLeaseOwnershipHandle OwnershipToken);

public sealed record TableLeaseHeartbeatRequest(TableLeaseOwnershipHandle OwnershipToken);

public sealed record TableLeaseFailRequest(
    TableLeaseOwnershipHandle OwnershipToken,
    string? Reason,
    string? ErrorCode,
    bool Requeue = true,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    public Error ToError()
    {
        var message = string.IsNullOrWhiteSpace(Reason) ? "lease failed" : Reason!;
        var code = string.IsNullOrWhiteSpace(ErrorCode) ? "error.tablelease.failed" : ErrorCode!;

        IReadOnlyDictionary<string, object?>? metadata = null;
        if (Metadata is { Count: > 0 })
        {
            metadata = Metadata.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase);
        }

        return Error.From(message, code, cause: null!, metadata);
    }
}

public sealed record TableLeaseDrainRequest;

public sealed record TableLeaseDrainResponse(IReadOnlyList<TableLeasePendingItemDto> Items);

public sealed record TableLeaseRestoreRequest(IReadOnlyList<TableLeasePendingItemDto> Items);

public sealed record TableLeaseRestoreResponse(int RestoredCount);

public sealed record TableLeaseQueueStats(long PendingCount, long ActiveLeaseCount);

public sealed record TableLeaseAcknowledgeResponse(bool Success, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static TableLeaseAcknowledgeResponse Ack() => new(true);

    public static TableLeaseAcknowledgeResponse NotFound(string code, string message) =>
        new(false, code, message);

    public static TableLeaseAcknowledgeResponse FromError(Error error) =>
        new(false, error.Code, error.Message);
}

public sealed record TableLeasePendingItemDto(
    TableLeaseItemPayload Payload,
    int Attempt,
    DateTimeOffset EnqueuedAt,
    TableLeaseErrorInfo? LastError,
    long SequenceId,
    TableLeaseOwnershipHandle? LastOwnershipToken)
{
    public static TableLeasePendingItemDto FromPending(TaskQueuePendingItem<TableLeaseWorkItem> pending)
    {
        var payload = pending.Value.ToPayload();
        var handle = pending.LastOwnershipToken.HasValue
            ? TableLeaseOwnershipHandle.FromToken(pending.LastOwnershipToken.Value)
            : null;

        return new TableLeasePendingItemDto(
            payload,
            pending.Attempt,
            pending.EnqueuedAt,
            TableLeaseErrorInfo.FromError(pending.LastError),
            pending.SequenceId,
            handle);
    }

    public TaskQueuePendingItem<TableLeaseWorkItem> ToPending()
    {
        var workItem = TableLeaseWorkItem.FromPayload(Payload);
        var lastToken = LastOwnershipToken?.ToToken();
        var error = LastError?.ToError() ?? Error.Unspecified("restored pending item");
        return new TaskQueuePendingItem<TableLeaseWorkItem>(
            workItem,
            Attempt,
            EnqueuedAt,
            error,
            SequenceId,
            lastToken);
    }
}

public sealed record TableLeaseOwnershipHandle(long SequenceId, int Attempt, Guid LeaseId)
{
    public TaskQueueOwnershipToken ToToken() => new(SequenceId, Attempt, LeaseId);

    public static TableLeaseOwnershipHandle FromToken(TaskQueueOwnershipToken token) =>
        new(token.SequenceId, token.Attempt, token.LeaseId);
}

public sealed record TableLeaseErrorInfo(string Message, string? Code)
{
    public static TableLeaseErrorInfo? FromError(Error? error)
    {
        if (error is null)
        {
            return null;
        }

        return new TableLeaseErrorInfo(error.Message, error.Code);
    }

    public Error ToError()
    {
        var code = string.IsNullOrWhiteSpace(Code) ? "error.tablelease.pending" : Code!;
        return Error.From(Message, code, cause: null!, metadata: null);
    }
}

public sealed record TableLeaseWorkItem(
    string Namespace,
    string Table,
    string PartitionKey,
    string PayloadEncoding,
    byte[] Body,
    ImmutableDictionary<string, string> Attributes,
    string? RequestId)
{
    public static TableLeaseWorkItem FromPayload(TableLeaseItemPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (string.IsNullOrWhiteSpace(payload.Namespace))
        {
            throw new ArgumentException("Namespace is required.", nameof(payload));
        }

        if (string.IsNullOrWhiteSpace(payload.Table))
        {
            throw new ArgumentException("Table is required.", nameof(payload));
        }

        if (string.IsNullOrWhiteSpace(payload.PartitionKey))
        {
            throw new ArgumentException("PartitionKey is required.", nameof(payload));
        }

        if (string.IsNullOrWhiteSpace(payload.PayloadEncoding))
        {
            throw new ArgumentException("Payload encoding is required.", nameof(payload));
        }

        var attributes = payload.Attributes is null
            ? ImmutableDictionary<string, string>.Empty
            : payload.Attributes.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

        var body = payload.Body ?? Array.Empty<byte>();

        return new TableLeaseWorkItem(
            payload.Namespace,
            payload.Table,
            payload.PartitionKey,
            payload.PayloadEncoding,
            body,
            attributes,
            payload.RequestId);
    }

    public TableLeaseItemPayload ToPayload()
    {
        var attributes = Attributes.Count == 0
            ? ImmutableDictionary<string, string>.Empty
            : Attributes;

        return new TableLeaseItemPayload(
            Namespace,
            Table,
            PartitionKey,
            PayloadEncoding,
            Body,
            attributes,
            RequestId);
    }
}
