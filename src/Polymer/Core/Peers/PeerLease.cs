using System;
using System.Threading.Tasks;

namespace Polymer.Core.Peers;

public sealed class PeerLease : IAsyncDisposable
{
    private readonly IPeer _peer;
    private bool _released;
    private bool _success;

    internal PeerLease(IPeer peer)
    {
        _peer = peer ?? throw new ArgumentNullException(nameof(peer));
        _success = false;
    }

    public IPeer Peer => _peer;

    public void MarkSuccess() => _success = true;

    public void MarkFailure() => _success = false;

    public ValueTask DisposeAsync()
    {
        if (_released)
        {
            return ValueTask.CompletedTask;
        }

        _released = true;
        _peer.Release(_success);
        return ValueTask.CompletedTask;
    }
}
