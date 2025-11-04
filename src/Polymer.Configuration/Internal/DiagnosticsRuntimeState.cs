using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polymer.Core.Diagnostics;

namespace Polymer.Configuration.Internal;

internal sealed class DiagnosticsRuntimeState : IDiagnosticsRuntime
{
    private readonly IOptionsMonitor<LoggerFilterOptions> _loggerFilterOptionsMonitor;
    private readonly IOptionsMonitorCache<LoggerFilterOptions> _loggerFilterOptionsCache;
    private readonly object _syncRoot = new();
    private readonly LogLevel _initialMinLevel;
    private LogLevel? _overrideMinLevel;
    private double? _traceSamplingProbability;

    public DiagnosticsRuntimeState(
        IOptionsMonitor<LoggerFilterOptions> loggerFilterOptionsMonitor,
        IOptionsMonitorCache<LoggerFilterOptions> loggerFilterOptionsCache)
    {
        _loggerFilterOptionsMonitor = loggerFilterOptionsMonitor;
        _loggerFilterOptionsCache = loggerFilterOptionsCache;
        _initialMinLevel = loggerFilterOptionsMonitor.CurrentValue.MinLevel;
    }

    public LogLevel? MinimumLogLevel
    {
        get
        {
            lock (_syncRoot)
            {
                return _overrideMinLevel;
            }
        }
    }

    public double? TraceSamplingProbability
    {
        get
        {
            lock (_syncRoot)
            {
                return _traceSamplingProbability;
            }
        }
    }

    public void SetMinimumLogLevel(LogLevel? level)
    {
        lock (_syncRoot)
        {
            _overrideMinLevel = level;

            var current = _loggerFilterOptionsMonitor.CurrentValue;
            var updated = new LoggerFilterOptions
            {
                MinLevel = level ?? _initialMinLevel
            };

            foreach (var rule in current.Rules)
            {
                updated.Rules.Add(rule);
            }

            _loggerFilterOptionsCache.TryRemove(Options.DefaultName);
            _loggerFilterOptionsCache.TryAdd(Options.DefaultName, updated);
        }
    }

    public void SetTraceSamplingProbability(double? probability)
    {
        if (probability is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(probability), "Sampling probability must be between 0.0 and 1.0.");
        }

        lock (_syncRoot)
        {
            _traceSamplingProbability = probability;
        }
    }
}
