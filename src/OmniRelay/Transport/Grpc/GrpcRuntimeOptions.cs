using Grpc.Core.Interceptors;
using OmniRelay.Transport.Http;

namespace OmniRelay.Transport.Grpc;

/// <summary>
/// Runtime options for gRPC clients including protocol, limits, keep-alive, and interceptors.
/// </summary>
public sealed record GrpcClientRuntimeOptions
{
    public bool EnableHttp3
    {
        get => field;
        init => field = value;
    }

    public Version? RequestVersion
    {
        get => field;
        init => field = value;
    }

    public HttpVersionPolicy? VersionPolicy
    {
        get => field;
        init => field = value;
    }

    public int? MaxReceiveMessageSize
    {
        get => field;
        init => field = value;
    }

    public int? MaxSendMessageSize
    {
        get => field;
        init => field = value;
    }

    public TimeSpan? KeepAlivePingDelay
    {
        get => field;
        init => field = value;
    }

    public TimeSpan? KeepAlivePingTimeout
    {
        get => field;
        init => field = value;
    }

    public HttpKeepAlivePingPolicy? KeepAlivePingPolicy
    {
        get => field;
        init => field = value;
    }

    public IReadOnlyList<Interceptor> Interceptors
    {
        get => field;
        init => field = value;
    } = [];

    public bool AllowHttp2Fallback
    {
        get => field;
        init => field = value;
    } = true;
}

/// <summary>
/// Runtime options for the gRPC server including message limits, errors, keep-alive, and HTTP/3.
/// </summary>
public sealed record GrpcServerRuntimeOptions
{
    public bool EnableHttp3
    {
        get => field;
        init => field = value;
    }

    public int? MaxReceiveMessageSize
    {
        get => field;
        init => field = value;
    }

    public int? MaxSendMessageSize
    {
        get => field;
        init => field = value;
    }

    public TimeSpan? KeepAlivePingDelay
    {
        get => field;
        init => field = value;
    }

    public TimeSpan? KeepAlivePingTimeout
    {
        get => field;
        init => field = value;
    }

    public bool? EnableDetailedErrors
    {
        get => field;
        init => field = value;
    }

    public IReadOnlyList<Type> Interceptors
    {
        get => field;
        init => field = value;
    } = [];

    public TimeSpan? ServerStreamWriteTimeout
    {
        get => field;
        init => field = value;
    }

    public TimeSpan? DuplexWriteTimeout
    {
        get => field;
        init => field = value;
    }

    public int? ServerStreamMaxMessageBytes
    {
        get => field;
        init => field = value;
    }

    public int? DuplexMaxMessageBytes
    {
        get => field;
        init => field = value;
    }

    public Http3RuntimeOptions? Http3
    {
        get => field;
        init => field = value;
    }
}
