using System.Collections.Immutable;
using System.Threading.Tasks;
using Hugo;
using OmniRelay.Errors;
using static Hugo.Go;

namespace OmniRelay.Core.Peers;

/// <summary>
/// Chooses the peer with the fewest in-flight requests, breaking ties randomly.
/// </summary>
public sealed class FewestPendingPeerChooser : IPeerChooser
{
    private readonly ImmutableArray<IPeer> _peers;
    private readonly Random _random;

    public FewestPendingPeerChooser(params IPeer[] peers)
        : this(peers is null ? throw new ArgumentNullException(nameof(peers)) : ImmutableArray.Create(peers))
    {
    }

    public FewestPendingPeerChooser(ImmutableArray<IPeer> peers, Random? random = null)
    {
        if (peers.IsDefaultOrEmpty)
        {
            throw new ArgumentException("At least one peer must be provided.", nameof(peers));
        }

        _peers = peers;
        _random = random ?? Random.Shared;
    }

    public async ValueTask<Result<PeerLease>> AcquireAsync(RequestMeta meta, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var waitDeadline = PeerChooserHelpers.ResolveDeadline(meta);

        while (true)
        {
            if (TryAcquireFromSnapshot(meta, cancellationToken, out var lease))
            {
                return Ok(lease!);
            }

            if (!PeerChooserHelpers.TryGetWaitDelay(waitDeadline, out var delay))
            {
                break;
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        PeerMetrics.RecordPoolExhausted(meta);
        var exhausted = OmniRelayErrorAdapter.FromStatus(
            OmniRelayStatusCode.ResourceExhausted,
            "All peers are busy.",
            transport: meta.Transport ?? "unknown");
        return Err<PeerLease>(exhausted);
    }

    private bool TryAcquireFromSnapshot(RequestMeta meta, CancellationToken cancellationToken, out PeerLease? lease)
    {
        var bestPeers = new List<IPeer>();
        var bestInflight = int.MaxValue;

        foreach (var peer in _peers)
        {
            var status = peer.Status;
            if (status.State != PeerState.Available)
            {
                continue;
            }

            if (status.Inflight < bestInflight)
            {
                bestInflight = status.Inflight;
                bestPeers.Clear();
                bestPeers.Add(peer);
            }
            else if (status.Inflight == bestInflight)
            {
                bestPeers.Add(peer);
            }
        }

        while (bestPeers.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var index = bestPeers.Count == 1
                ? 0
                : _random.Next(bestPeers.Count);
            var candidate = bestPeers[index];
            bestPeers.RemoveAt(index);

            if (candidate.TryAcquire(cancellationToken))
            {
                lease = new PeerLease(candidate, meta);
                return true;
            }

            PeerMetrics.RecordLeaseRejected(meta, candidate.Identifier, "busy");
        }

        lease = null;
        return false;
    }
}
