using System;
using System.Threading.RateLimiting;
using Polymer.Core;

namespace Polymer.Core.Middleware;

public sealed class RateLimitingOptions
{
    public RateLimiter? Limiter { get; init; }

    public Func<RequestMeta, RateLimiter?>? LimiterSelector { get; init; }
}
