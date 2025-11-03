using System;
using System.Collections.Generic;

namespace Polymer.Configuration.Models;

public sealed class PolymerConfigurationOptions
{
    public string? Service { get; set; }

    public InboundsConfiguration Inbounds { get; init; } = new();

    public IDictionary<string, ServiceOutboundConfiguration> Outbounds { get; init; } =
        new Dictionary<string, ServiceOutboundConfiguration>(StringComparer.OrdinalIgnoreCase);

    public MiddlewareConfiguration Middleware { get; init; } = new();

    public LoggingConfiguration Logging { get; init; } = new();
}

public sealed class InboundsConfiguration
{
    public IList<HttpInboundConfiguration> Http { get; } = new List<HttpInboundConfiguration>();

    public IList<GrpcInboundConfiguration> Grpc { get; } = new List<GrpcInboundConfiguration>();
}

public sealed class HttpInboundConfiguration
{
    public string? Name { get; set; }

    public IList<string> Urls { get; } = new List<string>();
}

public sealed class GrpcInboundConfiguration
{
    public string? Name { get; set; }

    public IList<string> Urls { get; } = new List<string>();

    public GrpcServerRuntimeConfiguration Runtime { get; init; } = new();

    public GrpcServerTlsConfiguration Tls { get; init; } = new();

    public GrpcTelemetryConfiguration Telemetry { get; init; } = new();
}

public sealed class GrpcServerRuntimeConfiguration
{
    public int? MaxReceiveMessageSize { get; set; }

    public int? MaxSendMessageSize { get; set; }

    public bool? EnableDetailedErrors { get; set; }

    public TimeSpan? KeepAlivePingDelay { get; set; }

    public TimeSpan? KeepAlivePingTimeout { get; set; }

    public IList<string> Interceptors { get; } = new List<string>();
}

public sealed class GrpcServerTlsConfiguration
{
    public string? CertificatePath { get; set; }

    public string? CertificatePassword { get; set; }

    public bool? CheckCertificateRevocation { get; set; }

    public string? ClientCertificateMode { get; set; }
}

public sealed class GrpcTelemetryConfiguration
{
    public bool? EnableServerLogging { get; set; }

    public bool? EnableClientLogging { get; set; }
}

public sealed class ServiceOutboundConfiguration
{
    public RpcOutboundConfiguration? Unary { get; set; }

    public RpcOutboundConfiguration? Oneway { get; set; }

    public RpcOutboundConfiguration? Stream { get; set; }

    public RpcOutboundConfiguration? ClientStream { get; set; }

    public RpcOutboundConfiguration? Duplex { get; set; }
}

public sealed class RpcOutboundConfiguration
{
    public IList<HttpOutboundTargetConfiguration> Http { get; } = new List<HttpOutboundTargetConfiguration>();

    public IList<GrpcOutboundTargetConfiguration> Grpc { get; } = new List<GrpcOutboundTargetConfiguration>();
}

public sealed class HttpOutboundTargetConfiguration
{
    public string? Key { get; set; }

    public string? Url { get; set; }

    public string? ClientName { get; set; }
}

public sealed class GrpcOutboundTargetConfiguration
{
    public string? Key { get; set; }

    public IList<string> Addresses { get; } = new List<string>();

    public string? RemoteService { get; set; }

    public string? PeerChooser { get; set; }

    public PeerSpecConfiguration? Peer { get; set; }

    public PeerCircuitBreakerConfiguration CircuitBreaker { get; init; } = new();

    public GrpcClientRuntimeConfiguration Runtime { get; init; } = new();

    public GrpcClientTlsConfiguration Tls { get; init; } = new();

    public GrpcTelemetryConfiguration Telemetry { get; init; } = new();
}

public sealed class PeerCircuitBreakerConfiguration
{
    public TimeSpan? BaseDelay { get; set; }

    public TimeSpan? MaxDelay { get; set; }

    public int? FailureThreshold { get; set; }

    public int? HalfOpenMaxAttempts { get; set; }

    public int? HalfOpenSuccessThreshold { get; set; }
}

public sealed class GrpcClientRuntimeConfiguration
{
    public int? MaxReceiveMessageSize { get; set; }

    public int? MaxSendMessageSize { get; set; }

    public TimeSpan? KeepAlivePingDelay { get; set; }

    public TimeSpan? KeepAlivePingTimeout { get; set; }

    public IList<string> Interceptors { get; } = new List<string>();
}

public sealed class GrpcClientTlsConfiguration
{
    public string? CertificatePath { get; set; }

    public string? CertificatePassword { get; set; }

    public string? TargetNameOverride { get; set; }

    public bool? AllowUntrustedCertificates { get; set; }
}

public sealed class MiddlewareConfiguration
{
    public MiddlewareStackConfiguration Inbound { get; init; } = new();

    public MiddlewareStackConfiguration Outbound { get; init; } = new();
}

public sealed class MiddlewareStackConfiguration
{
    public IList<string> Unary { get; } = new List<string>();

    public IList<string> Oneway { get; } = new List<string>();

    public IList<string> Stream { get; } = new List<string>();

    public IList<string> ClientStream { get; } = new List<string>();

    public IList<string> Duplex { get; } = new List<string>();
}

public sealed class LoggingConfiguration
{
    public string? Level { get; set; }

    public IDictionary<string, string> Overrides { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class PeerSpecConfiguration
{
    public string? Spec { get; set; }

    public IDictionary<string, string?> Settings { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
