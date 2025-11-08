using System.Collections.Immutable;
using System.Threading.Tasks;
using Hugo;
using OmniRelay.Errors;
using static Hugo.Go;

namespace OmniRelay.Core.Peers;

/// <summary>
/// Chooses peers in a round-robin fashion, skipping busy peers.
/// </summary>
public sealed class RoundRobinPeerChooser : IPeerChooser
{
    private readonly ImmutableArray<IPeer> _peers;
    private long _next = -1;

    public RoundRobinPeerChooser(params IPeer[] peers)
    {
        ArgumentNullException.ThrowIfNull(peers);

        _peers = [.. peers];
    }

    public RoundRobinPeerChooser(ImmutableArray<IPeer> peers)
    {
        _peers = peers;
    }

    public async ValueTask<Result<PeerLease>> AcquireAsync(RequestMeta meta, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_peers.IsDefaultOrEmpty)
        {
            var error = OmniRelayErrorAdapter.FromStatus(OmniRelayStatusCode.Unavailable, "No peers are registered for the requested service.", transport: meta.Transport ?? "unknown");
            return Err<PeerLease>(error);
        }

        var waitDeadline = PeerChooserHelpers.ResolveDeadline(meta);

        while (true)
        {
            var length = _peers.Length;
            for (var attempt = 0; attempt < length; attempt++)
            {
                var index = Interlocked.Increment(ref _next);
                var resolved = _peers[(int)(index % length)];

                if (resolved.TryAcquire(cancellationToken))
                {
                    return Ok(new PeerLease(resolved, meta));
                }

                PeerMetrics.RecordLeaseRejected(meta, resolved.Identifier, "busy");
            }

            if (!PeerChooserHelpers.TryGetWaitDelay(waitDeadline, out var delay))
            {
                break;
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        PeerMetrics.RecordPoolExhausted(meta);
        var exhausted = OmniRelayErrorAdapter.FromStatus(OmniRelayStatusCode.ResourceExhausted, "All peers are busy.", transport: meta.Transport ?? "unknown");
        return Err<PeerLease>(exhausted);
    }
}
