namespace OmniRelay.Core.Peers;

public interface IPeerTelemetry
{
    void RecordLeaseResult(bool success, double durationMilliseconds);
}
