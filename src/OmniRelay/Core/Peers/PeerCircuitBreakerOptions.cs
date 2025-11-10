namespace OmniRelay.Core.Peers;

/// <summary>
/// Configuration for the peer circuit breaker behavior and timings.
/// </summary>
public sealed class PeerCircuitBreakerOptions
{
    public TimeSpan BaseDelay
    {
        get => field;
        init => field = value;
    } = TimeSpan.FromMilliseconds(100);

    public TimeSpan MaxDelay
    {
        get => field;
        init => field = value;
    } = TimeSpan.FromSeconds(5);

    public int FailureThreshold
    {
        get => field;
        init => field = value;
    } = 1;

    public int HalfOpenMaxAttempts
    {
        get => field;
        init => field = value;
    } = 1;

    public int HalfOpenSuccessThreshold
    {
        get => field;
        init => field = value;
    } = 1;

    public TimeProvider TimeProvider
    {
        get => field;
        init => field = value;
    } = TimeProvider.System;
}
