using System.Diagnostics;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using OmniRelay.ControlPlane.ControlProtocol;
using OmniRelay.Protos.Control;

namespace OmniRelay.ControlPlane.Agent;

/// <summary>
/// Shared bootstrap/watch harness: load LKG, validate, apply, and resume watches with backoff.
/// </summary>
public sealed class WatchHarness
{
    private readonly IControlPlaneWatchClient _client;
    private readonly IControlPlaneConfigValidator _validator;
    private readonly IControlPlaneConfigApplier _applier;
    private readonly LkgCache _cache;
    private readonly TelemetryForwarder _telemetry;
    private readonly ILogger<WatchHarness> _logger;
    private readonly TimeSpan _backoffStart = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _backoffMax = TimeSpan.FromSeconds(30);
    private long _currentEpoch;

    public WatchHarness(
        IControlPlaneWatchClient client,
        IControlPlaneConfigValidator validator,
        IControlPlaneConfigApplier applier,
        LkgCache cache,
        TelemetryForwarder telemetry,
        ILogger<WatchHarness> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _applier = applier ?? throw new ArgumentNullException(nameof(applier));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(ControlWatchRequest request, CancellationToken cancellationToken)
    {
        // LKG bootstrap
        if (_cache.TryLoad(out var version, out var epoch, out var payload, out var resumeToken))
        {
            if (TryValidate(payload, out _))
            {
                await _applier.ApplyAsync(version, payload, cancellationToken).ConfigureAwait(false);
                _telemetry.RecordSnapshot(version);
                AgentLog.LkgApplied(_logger, version);
                _resumeToken = resumeToken;
                _currentEpoch = epoch;
            }
        }

        var backoff = _backoffStart;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var update in _client.WatchAsync(BuildRequest(request), cancellationToken).ConfigureAwait(false))
                {
                    backoff = _backoffStart; // reset on success
                    if (update.Error is not null && !string.IsNullOrWhiteSpace(update.Error.Code))
                    {
                        AgentLog.ControlWatchError(_logger, update.Error.Code, update.Error.Message);
                        backoff = await ApplyBackoffAsync(update.Backoff, backoff, cancellationToken).ConfigureAwait(false);
                        break;
                    }

                    AgentLog.ControlWatchResume(_logger, update.ResumeToken?.Version ?? update.Version, update.ResumeToken?.Epoch ?? 0);

                    if (!TryValidate(update.Payload.ToByteArray(), out var err))
                    {
                        AgentLog.ControlUpdateRejected(_logger, update.Version, err ?? "unknown");
                        continue;
                    }

                    var payloadBytes = update.Payload.ToByteArray();
                    await _applier.ApplyAsync(update.Version, payloadBytes, cancellationToken).ConfigureAwait(false);
                    var tokenBytes = update.ResumeToken?.ToByteArray() ?? Array.Empty<byte>();
                    _cache.Save(update.Version, update.Epoch, payloadBytes, tokenBytes);
                    _resumeToken = tokenBytes;
                    _currentEpoch = update.Epoch;
                    _telemetry.RecordSnapshot(update.Version);
                    AgentLog.ControlUpdateApplied(_logger, update.Version);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                AgentLog.ControlWatchFailed(_logger, ex);
                backoff = await ApplyBackoffAsync(null, backoff, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private byte[]? _resumeToken;

    private ControlWatchRequest BuildRequest(ControlWatchRequest template)
    {
        var request = template.Clone();
        if (_resumeToken is { Length: > 0 })
        {
            request.ResumeToken = WatchResumeToken.Parser.ParseFrom(_resumeToken);
        }

        return request;
    }

    private async Task<TimeSpan> ApplyBackoffAsync(ControlBackoff? backoff, TimeSpan current, CancellationToken cancellationToken)
    {
        var millis = backoff?.Millis ?? (int)current.TotalMilliseconds;
        AgentLog.ControlBackoffApplied(_logger, millis);
        var delay = TimeSpan.FromMilliseconds(millis);
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        var next = TimeSpan.FromMilliseconds(Math.Min(_backoffMax.TotalMilliseconds, Math.Max(millis * 2, _backoffStart.TotalMilliseconds)));
        return next;
    }

    private bool TryValidate(byte[] payload, out string? error)
    {
        var sw = Stopwatch.StartNew();
        var ok = _validator.Validate(payload, out error);
        AgentLog.ControlValidationResult(_logger, ok, sw.Elapsed.TotalMilliseconds);
        return ok;
    }
}
