using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using OpenTelemetry.Trace;

namespace OmniRelay.Core.Diagnostics;

/// <summary>
/// A sampler that consults <see cref="IDiagnosticsRuntime.TraceSamplingProbability"/> at runtime.
/// </summary>
public sealed class DiagnosticsRuntimeSampler : Sampler
{
    private readonly IDiagnosticsRuntime? _diagnosticsRuntime;
    private readonly Sampler _fallbackSampler;
    private CachedSampler? _cachedRatioSampler;

    public DiagnosticsRuntimeSampler(IDiagnosticsRuntime? diagnosticsRuntime, Sampler? fallbackSampler = null)
    {
        _diagnosticsRuntime = diagnosticsRuntime;
        _fallbackSampler = fallbackSampler ?? new AlwaysOnSampler();
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        var runtimeProbability = _diagnosticsRuntime?.TraceSamplingProbability;
        if (!runtimeProbability.HasValue)
        {
            return _fallbackSampler.ShouldSample(samplingParameters);
        }

        var probability = runtimeProbability.Value;

        if (probability <= 0d)
        {
            if (IsRecordedParent(samplingParameters.ParentContext) || HasRecordedLink(samplingParameters.Links))
            {
                return new SamplingResult(SamplingDecision.RecordAndSample);
            }

            return new SamplingResult(SamplingDecision.Drop);
        }

        if (probability >= 1d)
        {
            return _fallbackSampler.ShouldSample(samplingParameters);
        }

        var sampler = GetRatioSampler(probability);
        return sampler.ShouldSample(samplingParameters);
    }

    private static bool IsRecordedParent(ActivityContext parentContext) =>
        parentContext.TraceId != default && parentContext.TraceFlags.HasFlag(ActivityTraceFlags.Recorded);

    private static bool HasRecordedLink(IEnumerable<ActivityLink>? links)
    {
        if (links is null)
        {
            return false;
        }

        foreach (var link in links)
        {
            if (link.Context.TraceFlags.HasFlag(ActivityTraceFlags.Recorded))
            {
                return true;
            }
        }

        return false;
    }

    private Sampler GetRatioSampler(double probability)
    {
        var cache = Volatile.Read(ref _cachedRatioSampler);
        if (cache is not null && cache.Probability == probability)
        {
            return cache.Sampler;
        }

        var sampler = new TraceIdRatioBasedSampler(probability);
        cache = new CachedSampler(probability, sampler);
        Volatile.Write(ref _cachedRatioSampler, cache);
        return sampler;
    }

    private sealed record CachedSampler(double Probability, Sampler Sampler);
}
