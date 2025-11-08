using System;

namespace OmniRelay.Core.Peers;

/// <summary>
/// Shared utilities for peer choosers to compute wait deadlines and delays.
/// </summary>
internal static class PeerChooserHelpers
{
    private static readonly TimeSpan DefaultWaitSlice = TimeSpan.FromMilliseconds(5);

    /// <summary>
    /// Resolves the effective deadline for peer acquisition using the request metadata.
    /// Prefers an absolute deadline when provided and otherwise derives it from the time-to-live.
    /// </summary>
    public static DateTimeOffset? ResolveDeadline(RequestMeta meta)
    {
        ArgumentNullException.ThrowIfNull(meta);

        DateTimeOffset? resolved = null;

        if (meta.Deadline is { } absolute)
        {
            resolved = absolute.ToUniversalTime();
        }

        if (meta.TimeToLive is { } ttl && ttl > TimeSpan.Zero)
        {
            var ttlDeadline = DateTimeOffset.UtcNow.Add(ttl);
            resolved = resolved.HasValue && resolved.Value <= ttlDeadline ? resolved : ttlDeadline;
        }

        return resolved;
    }

    /// <summary>
    /// Computes the delay to wait before retrying peer acquisition.
    /// Returns <c>false</c> when the deadline has expired or no deadline is available.
    /// </summary>
    public static bool TryGetWaitDelay(DateTimeOffset? deadline, out TimeSpan delay)
    {
        if (deadline is not { } value)
        {
            delay = TimeSpan.Zero;
            return false;
        }

        var remaining = value - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
            return false;
        }

        delay = remaining <= DefaultWaitSlice ? remaining : DefaultWaitSlice;
        return true;
    }
}
