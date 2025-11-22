using Microsoft.Extensions.Logging;

namespace OmniRelay.Core.Extensions;

internal sealed class ExtensionTelemetryRecorder
{
    private readonly ILogger _logger;

    public ExtensionTelemetryRecorder(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordLoaded(ExtensionPackage package) => _logger.LogInformation("extension loaded: {Name} {Version} type={Type}", package.Name, package.Version, package.Type);
    public void RecordRejected(ExtensionPackage package, string reason) => _logger.LogWarning("extension rejected: {Name} {Version} reason={Reason}", package.Name, package.Version, reason);
    public void RecordFailed(ExtensionPackage package, string reason) => _logger.LogError("extension failure: {Name} {Version} reason={Reason}", package.Name, package.Version, reason);
    public void RecordWatchdogTrip(ExtensionPackage package, string resource) => _logger.LogWarning("extension watchdog trip: {Name} {Version} resource={Resource}", package.Name, package.Version, resource);
}
