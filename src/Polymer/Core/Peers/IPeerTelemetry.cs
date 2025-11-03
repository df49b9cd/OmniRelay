namespace Polymer.Core.Peers;

public interface IPeerTelemetry
{
    void RecordLeaseResult(bool success, double durationMilliseconds);
}
