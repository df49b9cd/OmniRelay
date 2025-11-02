using System;
using System.Collections.Generic;
using System.Threading;
using Polymer.Core;

namespace Polymer.Transport.Grpc;

public interface IGrpcPeerChooser
{
    Uri ChoosePeer(RequestMeta requestMeta, IReadOnlyList<Uri> peers);
}

public sealed class RoundRobinGrpcPeerChooser : IGrpcPeerChooser
{
    private int _nextIndex = -1;

    public Uri ChoosePeer(RequestMeta requestMeta, IReadOnlyList<Uri> peers)
    {
        if (peers is null)
        {
            throw new ArgumentNullException(nameof(peers));
        }

        if (peers.Count == 0)
        {
            throw new ArgumentException("At least one peer must be provided.", nameof(peers));
        }

        var index = Interlocked.Increment(ref _nextIndex);
        var resolvedIndex = Math.Abs(index) % peers.Count;
        return peers[resolvedIndex];
    }
}
