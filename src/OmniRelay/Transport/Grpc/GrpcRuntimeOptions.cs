using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    private IReadOnlyList<Type> _interceptors = [];

    public IReadOnlyList<Type> Interceptors
    {
        get => _interceptors;
        init => _interceptors = value ?? [];
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Interceptor types are explicitly referenced via typeof() so their constructors remain linked.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Interceptor types are explicitly referenced via typeof() so their constructors remain linked.")]
    internal IReadOnlyList<AnnotatedServerInterceptorType> AnnotatedInterceptors
    {
        get
        {
            if (_annotatedInterceptors is null)
            {
                var list = new List<AnnotatedServerInterceptorType>(_interceptors.Count);
                foreach (var type in _interceptors)
                {
                    if (type is not null)
                    {
                        var preserved = EnsureServerInterceptorType(type);
                        list.Add(new AnnotatedServerInterceptorType(preserved));
                    }
                }

                _annotatedInterceptors = list;
            }

            return _annotatedInterceptors;
        }
    }

    private IReadOnlyList<AnnotatedServerInterceptorType>? _annotatedInterceptors;

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
    private static Type EnsureServerInterceptorType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] Type type) =>
        type ?? throw new ArgumentNullException(nameof(type));
}

internal readonly struct AnnotatedServerInterceptorType
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    private readonly Type _type;

    public AnnotatedServerInterceptorType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] Type type) =>
        _type = type ?? throw new ArgumentNullException(nameof(type));

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    public Type Type => _type;
}
