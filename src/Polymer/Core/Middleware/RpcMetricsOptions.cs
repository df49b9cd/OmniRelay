using System.Diagnostics.Metrics;

namespace Polymer.Core.Middleware;

public sealed class RpcMetricsOptions
{
    /// <summary>
    /// Gets or sets the <see cref="Meter"/> used by the middleware. When unspecified, a shared meter named
    /// <c>Polymer.Rpc</c> is used.
    /// </summary>
    public Meter? Meter { get; init; }

    /// <summary>
    /// Gets or sets the base metric name prefix. Defaults to <c>polymer.rpc</c>.
    /// </summary>
    public string MetricPrefix { get; init; } = "polymer.rpc";
}
