using System;
using Hugo;
using Microsoft.Extensions.Logging;
using Polymer.Core;

namespace Polymer.Core.Middleware;

public sealed class RpcLoggingOptions
{
    public LogLevel SuccessLogLevel { get; init; } = LogLevel.Information;
    public LogLevel FailureLogLevel { get; init; } = LogLevel.Warning;
    public Func<RequestMeta, bool>? ShouldLogRequest { get; init; }
    public Func<Error, bool>? ShouldLogError { get; init; }
}
