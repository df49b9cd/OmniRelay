using Grpc.Core.Interceptors;

namespace OmniRelay.Transport.Grpc;

public sealed record GrpcClientRuntimeOptions
{
    public int? MaxReceiveMessageSize { get; init; }

    public int? MaxSendMessageSize { get; init; }

    public TimeSpan? KeepAlivePingDelay { get; init; }

    public TimeSpan? KeepAlivePingTimeout { get; init; }

    public HttpKeepAlivePingPolicy? KeepAlivePingPolicy { get; init; }

    public IReadOnlyList<Interceptor> Interceptors { get; init; } = [];
}

public sealed record GrpcServerRuntimeOptions
{
    public int? MaxReceiveMessageSize { get; init; }

    public int? MaxSendMessageSize { get; init; }

    public TimeSpan? KeepAlivePingDelay { get; init; }

    public TimeSpan? KeepAlivePingTimeout { get; init; }

    public bool? EnableDetailedErrors { get; init; }

    public IReadOnlyList<Type> Interceptors { get; init; } = [];

    public TimeSpan? ServerStreamWriteTimeout { get; init; }

    public TimeSpan? DuplexWriteTimeout { get; init; }

    public int? ServerStreamMaxMessageBytes { get; init; }

    public int? DuplexMaxMessageBytes { get; init; }
}
