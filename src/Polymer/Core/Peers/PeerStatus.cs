using System;

namespace Polymer.Core.Peers;

public readonly struct PeerStatus
{
    public PeerStatus(PeerState state, int inflight, DateTimeOffset? lastSuccess, DateTimeOffset? lastFailure)
    {
        State = state;
        Inflight = inflight;
        LastSuccess = lastSuccess;
        LastFailure = lastFailure;
    }

    public PeerState State { get; }

    public int Inflight { get; }

    public DateTimeOffset? LastSuccess { get; }

    public DateTimeOffset? LastFailure { get; }

    public static PeerStatus Unknown => new(PeerState.Unknown, 0, null, null);
}

public enum PeerState
{
    Unknown = 0,
    Available = 1,
    Unavailable = 2
}
