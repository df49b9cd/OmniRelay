using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace OmniRelay.Transport.Http;

/// <summary>
/// TLS configuration for the HTTP inbound server, including the certificate and client certificate policy.
/// </summary>
public sealed class HttpServerTlsOptions
{
    public X509Certificate2? Certificate
    {
        get => field;
        init => field = value;
    }

    public ClientCertificateMode ClientCertificateMode
    {
        get => field;
        init => field = value;
    } = ClientCertificateMode.NoCertificate;

    public bool? CheckCertificateRevocation
    {
        get => field;
        init => field = value;
    }
}

/// <summary>
/// Kestrel runtime options for the HTTP inbound server such as request limits and timeouts, with optional HTTP/3 settings.
/// </summary>
public sealed class HttpServerRuntimeOptions
{
    public bool EnableHttp3
    {
        get => field;
        set => field = value;
    }

    public long? MaxRequestBodySize
    {
        get => field;
        set => field = value;
    }

    public long? MaxInMemoryDecodeBytes
    {
        get => field;
        set => field = value;
    }

    public int? MaxRequestLineSize
    {
        get => field;
        set => field = value;
    }

    public int? MaxRequestHeadersTotalSize
    {
        get => field;
        set => field = value;
    }

    public TimeSpan? KeepAliveTimeout
    {
        get => field;
        set => field = value;
    }

    public TimeSpan? RequestHeadersTimeout
    {
        get => field;
        set => field = value;
    }

    public TimeSpan? ServerStreamWriteTimeout
    {
        get => field;
        set => field = value;
    }

    public TimeSpan? DuplexWriteTimeout
    {
        get => field;
        set => field = value;
    }

    public int? ServerStreamMaxMessageBytes
    {
        get => field;
        set => field = value;
    }

    public int? DuplexMaxFrameBytes
    {
        get => field;
        set => field = value;
    }

    public Http3RuntimeOptions? Http3
    {
        get => field;
        set => field = value;
    }
}

/// <summary>
/// Additional HTTP/3 (QUIC) runtime tuning options for the inbound server.
/// </summary>
public sealed class Http3RuntimeOptions
{
    public bool? EnableAltSvc
    {
        get => field;
        init => field = value;
    }

    public TimeSpan? IdleTimeout
    {
        get => field;
        init => field = value;
    }

    public TimeSpan? KeepAliveInterval
    {
        get => field;
        init => field = value;
    }

    public int? MaxBidirectionalStreams
    {
        get => field;
        init => field = value;
    }

    public int? MaxUnidirectionalStreams
    {
        get => field;
        init => field = value;
    }
}
