using System.Threading;
using System.Threading.Tasks;
using Hugo;
using Polymer.Core;

namespace Polymer.Core.Peers;

public interface IPeerChooser
{
    ValueTask<Result<PeerLease>> AcquireAsync(RequestMeta meta, CancellationToken cancellationToken = default);
}
