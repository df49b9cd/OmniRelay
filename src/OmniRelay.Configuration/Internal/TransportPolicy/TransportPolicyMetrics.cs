using System.Diagnostics.Metrics;

namespace OmniRelay.Configuration.Internal.TransportPolicy;

internal static class TransportPolicyMetrics
{
    private const string MeterName = "OmniRelay.Transport.Policy";
    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> EndpointEvaluations = Meter.CreateCounter<long>(
        "omnirelay.transport.policy.endpoints",
        description: "Total number of endpoints evaluated against the transport policy.");

    private static readonly Counter<long> PolicyViolations = Meter.CreateCounter<long>(
        "omnirelay.transport.policy.violations",
        description: "Count of endpoints that violated transport policy requirements.");

    private static readonly Counter<long> PolicyExceptions = Meter.CreateCounter<long>(
        "omnirelay.transport.policy.exceptions",
        description: "Count of endpoints permitted via explicit transport policy exceptions.");

    public static void RecordEvaluation(TransportPolicyFinding finding)
    {
        if (finding is null)
        {
            return;
        }

        var tags = BuildTags(finding);
        EndpointEvaluations.Add(1, tags);
        switch (finding.Status)
        {
            case TransportPolicyFindingStatus.ViolatesPolicy:
                PolicyViolations.Add(1, tags);
                break;
            case TransportPolicyFindingStatus.Excepted:
                PolicyExceptions.Add(1, tags);
                break;
        }
    }

    private static KeyValuePair<string, object?>[] BuildTags(TransportPolicyFinding finding)
    {
        return
        [
            KeyValuePair.Create<string, object?>("omnirelay.transport.endpoint", finding.Endpoint),
            KeyValuePair.Create<string, object?>("omnirelay.transport.category", finding.Category),
            KeyValuePair.Create<string, object?>("omnirelay.transport.protocol", finding.Transport),
            KeyValuePair.Create<string, object?>("omnirelay.transport.encoding", finding.Encoding),
            KeyValuePair.Create<string, object?>("omnirelay.transport.status", finding.Status.ToString().ToLowerInvariant())
        ];
    }
}
