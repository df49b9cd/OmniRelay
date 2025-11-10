using System.Collections.Immutable;

namespace OmniRelay.Core.Peers;

/// <summary>
/// Snapshot describing per-peer SafeTaskQueue lease health.
/// </summary>
public sealed record PeerLeaseHealthSnapshot(
    string PeerId,
    DateTimeOffset LastHeartbeat,
    DateTimeOffset? LastDisconnect,
    bool IsHealthy,
    int ActiveAssignments,
    int PendingReassignments,
    ImmutableArray<PeerLeaseHandle> ActiveLeases,
    ImmutableDictionary<string, string> Metadata)
{
    public string PeerId
    {
        get => field;
        init => field = value;
    } = PeerId;

    public DateTimeOffset LastHeartbeat
    {
        get => field;
        init => field = value;
    } = LastHeartbeat;

    public DateTimeOffset? LastDisconnect
    {
        get => field;
        init => field = value;
    } = LastDisconnect;

    public bool IsHealthy
    {
        get => field;
        init => field = value;
    } = IsHealthy;

    public int ActiveAssignments
    {
        get => field;
        init => field = value;
    } = ActiveAssignments;

    public int PendingReassignments
    {
        get => field;
        init => field = value;
    } = PendingReassignments;

    public ImmutableArray<PeerLeaseHandle> ActiveLeases
    {
        get => field;
        init => field = value;
    } = ActiveLeases;

    public ImmutableDictionary<string, string> Metadata
    {
        get => field;
        init => field = value;
    } = Metadata;
}
