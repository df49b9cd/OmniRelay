using System.Threading;

namespace Polymer.Core.Peers;

public interface IPeer
{
    string Identifier { get; }

    PeerStatus Status { get; }

    bool TryAcquire(CancellationToken cancellationToken = default);

    void Release(bool success);
}
