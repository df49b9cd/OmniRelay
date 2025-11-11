using Hugo;
using OmniRelay.Core.Transport;

namespace OmniRelay.Core.Middleware;

/// <summary>Unary outbound middleware contract.</summary>
public interface IUnaryOutboundMiddleware
{
    /// <summary>Invokes the middleware for a unary outbound request.</summary>
    ValueTask<Result<Response<ReadOnlyMemory<byte>>>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        UnaryOutboundHandler next);
}

/// <summary>Unary inbound middleware contract.</summary>
public interface IUnaryInboundMiddleware
{
    /// <summary>Invokes the middleware for a unary inbound request.</summary>
    ValueTask<Result<Response<ReadOnlyMemory<byte>>>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        UnaryInboundHandler next);
}

/// <summary>Oneway outbound middleware contract.</summary>
public interface IOnewayOutboundMiddleware
{
    /// <summary>Invokes the middleware for an oneway outbound request.</summary>
    ValueTask<Result<OnewayAck>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        OnewayOutboundHandler next);
}

/// <summary>Oneway inbound middleware contract.</summary>
public interface IOnewayInboundMiddleware
{
    /// <summary>Invokes the middleware for an oneway inbound request.</summary>
    ValueTask<Result<OnewayAck>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        OnewayInboundHandler next);
}

/// <summary>Server-streaming outbound middleware contract.</summary>
public interface IStreamOutboundMiddleware
{
    /// <summary>Invokes the middleware for a streaming outbound request.</summary>
    ValueTask<Result<IStreamCall>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        StreamCallOptions options,
        CancellationToken cancellationToken,
        StreamOutboundHandler next);
}

/// <summary>Server-streaming inbound middleware contract.</summary>
public interface IStreamInboundMiddleware
{
    /// <summary>Invokes the middleware for a streaming inbound request.</summary>
    ValueTask<Result<IStreamCall>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        StreamCallOptions options,
        CancellationToken cancellationToken,
        StreamInboundHandler next);
}

/// <summary>Client-stream inbound middleware contract.</summary>
public interface IClientStreamInboundMiddleware
{
    /// <summary>Invokes the middleware for a client-stream inbound request.</summary>
    ValueTask<Result<Response<ReadOnlyMemory<byte>>>> InvokeAsync(
        ClientStreamRequestContext context,
        CancellationToken cancellationToken,
        ClientStreamInboundHandler next);
}

/// <summary>Client-stream outbound middleware contract.</summary>
public interface IClientStreamOutboundMiddleware
{
    /// <summary>Invokes the middleware for a client-stream outbound request.</summary>
    ValueTask<Result<IClientStreamTransportCall>> InvokeAsync(
        RequestMeta requestMeta,
        CancellationToken cancellationToken,
        ClientStreamOutboundHandler next);
}

/// <summary>Duplex inbound middleware contract.</summary>
public interface IDuplexInboundMiddleware
{
    /// <summary>Invokes the middleware for a duplex inbound request.</summary>
    ValueTask<Result<IDuplexStreamCall>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        DuplexInboundHandler next);
}

/// <summary>Duplex outbound middleware contract.</summary>
public interface IDuplexOutboundMiddleware
{
    /// <summary>Invokes the middleware for a duplex outbound request.</summary>
    ValueTask<Result<IDuplexStreamCall>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        DuplexOutboundHandler next);
}
