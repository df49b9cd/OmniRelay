using System.Diagnostics.Tracing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OmniRelay.Core.Diagnostics;

/// <summary>
/// Bridges QUIC/MsQuic and Kestrel EventSource events into structured logs so operators can observe
/// connection lifecycle (handshake failures, migration, congestion) without external ETW tooling.
/// </summary>
internal sealed class QuicKestrelEventBridge(ILogger<QuicKestrelEventBridge> logger) : EventListener
{
    private readonly ILogger<QuicKestrelEventBridge> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private const string MsQuicEventSource = "Private.InternalDiagnostics.System.Net.Quic";
    private const string KestrelEventSource = "Microsoft-AspNetCore-Server-Kestrel";

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        base.OnEventSourceCreated(eventSource);

        if (eventSource.Name.Equals(MsQuicEventSource, StringComparison.Ordinal) ||
            eventSource.Name.Equals(KestrelEventSource, StringComparison.Ordinal))
        {
            // Enable informational+ events; adjust keywords as needed for verbosity
            EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData is null)
        {
            return;
        }

        try
        {
            var source = eventData.EventSource?.Name ?? "unknown";
            var name = eventData.EventName ?? eventData.EventId.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = source,
                ["event"] = name,
                ["event_id"] = eventData.EventId,
                ["level"] = eventData.Level.ToString()
            };

            if (eventData.PayloadNames is { Count: > 0 } && eventData.Payload is { Count: > 0 })
            {
                for (var i = 0; i < Math.Min(eventData.PayloadNames.Count, eventData.Payload.Count); i++)
                {
                    var key = eventData.PayloadNames[i];
                    var value = eventData.Payload[i];
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        payload[key] = value;
                    }
                }
            }

            // Derive a coarse category for common lifecycle events to assist filtering
            var category = ClassifyEvent(source, name, payload);

            using var scope = _logger.BeginScope(payload);

            switch (category)
            {
                case "handshake_failure":
                    _logger.LogWarning("quic event: category={Category}", category);
                    break;
                case "migration":
                case "congestion":
                    _logger.LogInformation("quic event: category={Category}", category);
                    break;
                default:
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("quic event: category={Category}", category);
                    }
                    break;
            }
        }
        catch
        {
            // Swallow logging errors to avoid impacting application flow
        }
    }

    private static string ClassifyEvent(string provider, string name, IReadOnlyDictionary<string, object?> payload)
    {
        // Heuristic classification based on common keywords
        var key = provider + ":" + name;

        var text = string.Join(' ', payload.Select(kv => kv.Key + "=" + (kv.Value?.ToString() ?? string.Empty))).ToLowerInvariant();

        if (text.Contains("handshake") && (text.Contains("fail") || text.Contains("error")))
        {
            return "handshake_failure";
        }

        if (text.Contains("migrate") || text.Contains("path_validated"))
        {
            return "migration";
        }

        if (text.Contains("congestion") || text.Contains("loss") || text.Contains("retransmit"))
        {
            return "congestion";
        }

        if (key.Contains("Kestrel", StringComparison.OrdinalIgnoreCase) && name.Contains("Http3", StringComparison.OrdinalIgnoreCase))
        {
            return "http3";
        }

        return "other";
    }
}

/// <summary>
/// Hosted service to control the lifetime of the QuicKestrelEventBridge.
/// </summary>
internal sealed class QuicDiagnosticsHostedService(ILogger<QuicKestrelEventBridge> logger) : IHostedService
{
    private readonly QuicKestrelEventBridge _bridge = new QuicKestrelEventBridge(logger);

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _bridge.Dispose();
        return Task.CompletedTask;
    }
}
