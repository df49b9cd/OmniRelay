using Microsoft.Extensions.Logging;

namespace OmniRelay.Core.Diagnostics;

public interface IDiagnosticsRuntime
{
    LogLevel? MinimumLogLevel { get; }

    double? TraceSamplingProbability { get; }

    void SetMinimumLogLevel(LogLevel? level);

    void SetTraceSamplingProbability(double? probability);
}
