namespace OmniRelay.Dispatcher;

/// <summary>Describes the current backpressure state observed on the resource lease queue.</summary>
public sealed record ResourceLeaseBackpressureSignal(
    bool IsActive,
    long PendingCount,
    DateTimeOffset ObservedAt,
    long? HighWatermark,
    long? LowWatermark)
{
    public bool IsActive
    {
        get => field;
        init => field = value;
    } = IsActive;

    public long PendingCount
    {
        get => field;
        init => field = value;
    } = PendingCount;

    public DateTimeOffset ObservedAt
    {
        get => field;
        init => field = value;
    } = ObservedAt;

    public long? HighWatermark
    {
        get => field;
        init => field = value;
    } = HighWatermark;

    public long? LowWatermark
    {
        get => field;
        init => field = value;
    } = LowWatermark;
}

/// <summary>Consumers implement this to adjust throttling or instrumentation when backpressure toggles.</summary>
public interface IResourceLeaseBackpressureListener
{
    ValueTask OnBackpressureChanged(ResourceLeaseBackpressureSignal signal, CancellationToken cancellationToken);
}
