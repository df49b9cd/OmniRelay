using Hugo;

namespace OmniRelay.Core.Peers;

public interface IPeerChooser
{
    ValueTask<Result<PeerLease>> AcquireAsync(RequestMeta meta, CancellationToken cancellationToken = default);
}
