using System.Threading.RateLimiting;

namespace Polymer.Core.Middleware;

public sealed class RateLimitingOptions
{
    public RateLimiter? Limiter { get; init; }

    public Func<RequestMeta, RateLimiter?>? LimiterSelector { get; init; }
}
