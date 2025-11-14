namespace OmniRelay.Configuration.Models;

/// <summary>Configures the dedicated diagnostics/leadership control-plane hosts.</summary>
public sealed class DiagnosticsControlPlaneConfiguration
{
    /// <summary>HTTP control-plane bindings. Defaults to the classic dispatcher diagnostics endpoint.</summary>
    public List<string> HttpUrls { get; set; } = ["http://127.0.0.1:8080"];

    public HttpServerRuntimeConfiguration HttpRuntime { get; init; } = new();

    public HttpServerTlsConfiguration HttpTls { get; init; } = new();

    /// <summary>gRPC control-plane bindings. Defaults to disabled until configured.</summary>
    public List<string> GrpcUrls { get; set; } = [];

    public GrpcServerRuntimeConfiguration GrpcRuntime { get; init; } = new();

    public GrpcServerTlsConfiguration GrpcTls { get; init; } = new();

    /// <summary>Optional shared TLS configuration used by both hosts.</summary>
    public TransportTlsConfiguration Tls { get; init; } = new();
}
