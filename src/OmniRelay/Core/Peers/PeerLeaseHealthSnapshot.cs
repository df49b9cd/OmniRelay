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
    ImmutableDictionary<string, string> Metadata);
