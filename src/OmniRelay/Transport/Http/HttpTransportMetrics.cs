using System.Diagnostics.Metrics;

namespace OmniRelay.Transport.Http;

internal static class HttpTransportMetrics
{
    public const string MeterName = "OmniRelay.Transport.Http";
    private static readonly Meter Meter = new(MeterName);

    // Generic request metrics (applies to unary and streaming endpoints)
    public static readonly Counter<long> RequestsStarted =
        Meter.CreateCounter<long>("omnirelay.http.requests.started", description: "HTTP requests started by OmniRelay inbound.");

    public static readonly Counter<long> RequestsCompleted =
        Meter.CreateCounter<long>("omnirelay.http.requests.completed", description: "HTTP requests completed by OmniRelay inbound.");

    public static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("omnirelay.http.request.duration", unit: "ms", description: "HTTP request duration in milliseconds measured at OmniRelay inbound.");

    // Client-side: count when HTTP/3 was desired but a lower protocol was used
    public static readonly Counter<long> ClientProtocolFallbacks =
        Meter.CreateCounter<long>("omnirelay.http.client.fallbacks", description: "HTTP client fallbacks when HTTP/3 was desired but a lower protocol was used.");

    public static KeyValuePair<string, object?>[] CreateBaseTags(
        string service,
        string procedure,
        string method,
        string protocol)
    {
        var tags = new List<KeyValuePair<string, object?>>(8)
        {
            KeyValuePair.Create<string, object?>("rpc.system", "http"),
            KeyValuePair.Create<string, object?>("rpc.service", service ?? string.Empty),
            KeyValuePair.Create<string, object?>("rpc.procedure", procedure ?? string.Empty),
            KeyValuePair.Create<string, object?>("http.request.method", method ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(protocol))
        {
            tags.Add(KeyValuePair.Create<string, object?>("rpc.protocol", protocol));

            if (protocol.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add(KeyValuePair.Create<string, object?>("network.protocol.name", "http"));
                var version = protocol.Length > 5 ? protocol[5..] : string.Empty;
                if (!string.IsNullOrEmpty(version))
                {
                    tags.Add(KeyValuePair.Create<string, object?>("network.protocol.version", version));
                }

                tags.Add(KeyValuePair.Create<string, object?>("network.transport", version.StartsWith("3", StringComparison.Ordinal) ? "quic" : "tcp"));
            }
        }

        return [.. tags];
    }

    public static KeyValuePair<string, object?>[] AppendOutcome(
        KeyValuePair<string, object?>[] baseTags,
        int? httpStatus,
        string outcome)
    {
        var size = baseTags.Length + (httpStatus.HasValue ? 2 : 1);
        var tags = new KeyValuePair<string, object?>[size];
        Array.Copy(baseTags, tags, baseTags.Length);
        var index = baseTags.Length;
        if (httpStatus.HasValue)
        {
            tags[index++] = KeyValuePair.Create<string, object?>("http.response.status_code", httpStatus.Value);
        }
        tags[index] = KeyValuePair.Create<string, object?>("outcome", outcome);
        return tags;
    }

    public static KeyValuePair<string, object?>[] AppendObservedProtocol(
        KeyValuePair<string, object?>[] baseTags,
        string? observedProtocol)
    {
        if (string.IsNullOrWhiteSpace(observedProtocol))
        {
            return baseTags;
        }

        var tags = new KeyValuePair<string, object?>[baseTags.Length + 1];
        Array.Copy(baseTags, tags, baseTags.Length);
        tags[^1] = KeyValuePair.Create<string, object?>("http.observed_protocol", observedProtocol);
        return tags;
    }
}
