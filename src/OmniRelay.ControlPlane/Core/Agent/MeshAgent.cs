using Microsoft.Extensions.Logging;
using OmniRelay.ControlPlane.ControlProtocol;
using OmniRelay.Protos.Control;
using OmniRelay.Core.Transport;

namespace OmniRelay.ControlPlane.Agent;

/// <summary>Minimal MeshKit agent: watches control-plane, applies LKG caching, emits telemetry.</summary>
public sealed class MeshAgent : ILifecycle, IDisposable
{
    private readonly IControlPlaneWatchClient _client;
    private readonly LkgCache _cache;
    private readonly TelemetryForwarder _telemetry;
    private readonly ILogger<MeshAgent> _logger;
    private CancellationTokenSource? _cts;
    private Task? _watchTask;

    public MeshAgent(IControlPlaneWatchClient client, LkgCache cache, TelemetryForwarder telemetry, ILogger<MeshAgent> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (_watchTask is not null)
        {
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var request = new ControlWatchRequest { NodeId = Environment.MachineName };
        _watchTask = Task.Run(() => WatchLoopAsync(request, _cts.Token), _cts.Token);
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        if (_watchTask is not null)
        {
            try
            {
                await _watchTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _watchTask = null;
        _cts.Dispose();
        _cts = null;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private async Task WatchLoopAsync(ControlWatchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var update in _client.WatchAsync(request, cancellationToken))
            {
                var payload = update.Payload.ToByteArray();
                _cache.Save(update.Version, payload);
                _telemetry.RecordSnapshot(update.Version);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "agent watch failed");
        }
    }
}
