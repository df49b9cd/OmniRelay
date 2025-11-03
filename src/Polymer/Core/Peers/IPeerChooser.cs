using Hugo;

namespace Polymer.Core.Peers;

public interface IPeerChooser
{
    ValueTask<Result<PeerLease>> AcquireAsync(RequestMeta meta, CancellationToken cancellationToken = default);
}
