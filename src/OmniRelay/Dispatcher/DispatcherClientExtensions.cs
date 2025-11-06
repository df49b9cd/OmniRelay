using OmniRelay.Core;
using OmniRelay.Core.Clients;

namespace OmniRelay.Dispatcher;

/// <summary>
/// Factory helpers to create typed RPC clients from a configured <see cref="Dispatcher"/>.
/// </summary>
public static class DispatcherClientExtensions
{
    /// <summary>
    /// Creates a typed unary client using an explicit codec and an optional outbound key.
    /// </summary>
    public static UnaryClient<TRequest, TResponse> CreateUnaryClient<TRequest, TResponse>(
        this Dispatcher dispatcher,
        string service,
        ICodec<TRequest, TResponse> codec,
        string? outboundKey = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        ArgumentNullException.ThrowIfNull(codec);

        var configuration = dispatcher.ClientConfig(service);
        if (!configuration.TryGetUnary(outboundKey, out var outbound) || outbound is null)
        {
            throw new KeyNotFoundException($"No unary outbound registered for service '{service}' with key '{outboundKey ?? OutboundCollection.DefaultKey}'.");
        }

        return new UnaryClient<TRequest, TResponse>(outbound, codec, configuration.UnaryMiddleware);
    }

    /// <summary>
    /// Creates a typed unary client by resolving a registered outbound codec for a procedure.
    /// </summary>
    public static UnaryClient<TRequest, TResponse> CreateUnaryClient<TRequest, TResponse>(
        this Dispatcher dispatcher,
        string service,
        string procedure,
        string? outboundKey = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (!dispatcher.Codecs.TryResolve<TRequest, TResponse>(ProcedureCodecScope.Outbound, service, procedure, ProcedureKind.Unary, out var codec))
        {
            throw new KeyNotFoundException($"No outbound codec registered for service '{service}' procedure '{procedure}' ({ProcedureKind.Unary}).");
        }

        return dispatcher.CreateUnaryClient(service, codec, outboundKey);
    }

    /// <summary>
    /// Creates a typed oneway client using an explicit codec and an optional outbound key.
    /// </summary>
    public static OnewayClient<TRequest> CreateOnewayClient<TRequest>(
        this Dispatcher dispatcher,
        string service,
        ICodec<TRequest, object> codec,
        string? outboundKey = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        ArgumentNullException.ThrowIfNull(codec);

        var configuration = dispatcher.ClientConfig(service);
        if (!configuration.TryGetOneway(outboundKey, out var outbound) || outbound is null)
        {
            throw new KeyNotFoundException($"No oneway outbound registered for service '{service}' with key '{outboundKey ?? OutboundCollection.DefaultKey}'.");
        }

        return new OnewayClient<TRequest>(outbound, codec, configuration.OnewayMiddleware);
    }

    /// <summary>
    /// Creates a typed oneway client by resolving a registered outbound codec for a procedure.
    /// </summary>
    public static OnewayClient<TRequest> CreateOnewayClient<TRequest>(
        this Dispatcher dispatcher,
        string service,
        string procedure,
        string? outboundKey = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (!dispatcher.Codecs.TryResolve<TRequest, object>(ProcedureCodecScope.Outbound, service, procedure, ProcedureKind.Oneway, out var codec))
        {
            throw new KeyNotFoundException($"No outbound codec registered for service '{service}' procedure '{procedure}' ({ProcedureKind.Oneway}).");
        }

        return dispatcher.CreateOnewayClient(service, codec, outboundKey);
    }

    /// <summary>
    /// Creates a typed server-stream client using an explicit codec and an optional outbound key.
    /// </summary>
    public static StreamClient<TRequest, TResponse> CreateStreamClient<TRequest, TResponse>(
        this Dispatcher dispatcher,
        string service,
        ICodec<TRequest, TResponse> codec,
        string? outboundKey = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        ArgumentNullException.ThrowIfNull(codec);

        var configuration = dispatcher.ClientConfig(service);
        if (!configuration.TryGetStream(outboundKey, out var outbound) || outbound is null)
        {
            throw new KeyNotFoundException($"No stream outbound registered for service '{service}' with key '{outboundKey ?? OutboundCollection.DefaultKey}'.");
        }

        return new StreamClient<TRequest, TResponse>(outbound, codec, configuration.StreamMiddleware);
    }

    /// <summary>
    /// Creates a typed server-stream client by resolving a registered outbound codec for a procedure.
    /// </summary>
    public static StreamClient<TRequest, TResponse> CreateStreamClient<TRequest, TResponse>(
        this Dispatcher dispatcher,
        string service,
        string procedure,
        string? outboundKey = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (!dispatcher.Codecs.TryResolve<TRequest, TResponse>(ProcedureCodecScope.Outbound, service, procedure, ProcedureKind.Stream, out var codec))
        {
            throw new KeyNotFoundException($"No outbound codec registered for service '{service}' procedure '{procedure}' ({ProcedureKind.Stream}).");
        }

        return dispatcher.CreateStreamClient(service, codec, outboundKey);
    }

    /// <summary>
    /// Creates a typed client-stream client using an explicit codec and an optional outbound key.
    /// </summary>
    public static ClientStreamClient<TRequest, TResponse> CreateClientStreamClient<TRequest, TResponse>(
        this Dispatcher dispatcher,
        string service,
        ICodec<TRequest, TResponse> codec,
        string? outboundKey = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        ArgumentNullException.ThrowIfNull(codec);

        var configuration = dispatcher.ClientConfig(service);
        if (!configuration.TryGetClientStream(outboundKey, out var outbound) || outbound is null)
        {
            throw new KeyNotFoundException($"No client stream outbound registered for service '{service}' with key '{outboundKey ?? OutboundCollection.DefaultKey}'.");
        }

        return new ClientStreamClient<TRequest, TResponse>(outbound, codec, configuration.ClientStreamMiddleware);
    }

    /// <summary>
    /// Creates a typed client-stream client by resolving a registered outbound codec for a procedure.
    /// </summary>
    public static ClientStreamClient<TRequest, TResponse> CreateClientStreamClient<TRequest, TResponse>(
        this Dispatcher dispatcher,
        string service,
        string procedure,
        string? outboundKey = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (!dispatcher.Codecs.TryResolve<TRequest, TResponse>(ProcedureCodecScope.Outbound, service, procedure, ProcedureKind.ClientStream, out var codec))
        {
            throw new KeyNotFoundException($"No outbound codec registered for service '{service}' procedure '{procedure}' ({ProcedureKind.ClientStream}).");
        }

        return dispatcher.CreateClientStreamClient(service, codec, outboundKey);
    }

    /// <summary>
    /// Creates a typed duplex-stream client using an explicit codec and an optional outbound key.
    /// </summary>
    public static DuplexStreamClient<TRequest, TResponse> CreateDuplexStreamClient<TRequest, TResponse>(
        this Dispatcher dispatcher,
        string service,
        ICodec<TRequest, TResponse> codec,
        string? outboundKey = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        ArgumentNullException.ThrowIfNull(codec);

        var configuration = dispatcher.ClientConfig(service);
        if (!configuration.TryGetDuplex(outboundKey, out var outbound) || outbound is null)
        {
            throw new KeyNotFoundException($"No duplex stream outbound registered for service '{service}' with key '{outboundKey ?? OutboundCollection.DefaultKey}'.");
        }

        return new DuplexStreamClient<TRequest, TResponse>(outbound, codec, configuration.DuplexMiddleware);
    }

    /// <summary>
    /// Creates a typed duplex-stream client by resolving a registered outbound codec for a procedure.
    /// </summary>
    public static DuplexStreamClient<TRequest, TResponse> CreateDuplexStreamClient<TRequest, TResponse>(
        this Dispatcher dispatcher,
        string service,
        string procedure,
        string? outboundKey = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (!dispatcher.Codecs.TryResolve<TRequest, TResponse>(ProcedureCodecScope.Outbound, service, procedure, ProcedureKind.Duplex, out var codec))
        {
            throw new KeyNotFoundException($"No outbound codec registered for service '{service}' procedure '{procedure}' ({ProcedureKind.Duplex}).");
        }

        return dispatcher.CreateDuplexStreamClient(service, codec, outboundKey);
    }
}
