using Hugo;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using static Hugo.Go;

namespace OmniRelay.Core.Clients;

/// <summary>
/// Typed oneway RPC client that applies middleware and uses an <see cref="ICodec{TRequest, TResponse}"/> for request encoding.
/// </summary>
public sealed class OnewayClient<TRequest>
{
    private readonly OnewayOutboundDelegate _pipeline;
    private readonly ICodec<TRequest, object> _codec;

    /// <summary>
    /// Creates a oneway client bound to an outbound and codec.
    /// </summary>
    public OnewayClient(
        IOnewayOutbound outbound,
        ICodec<TRequest, object> codec,
        IReadOnlyList<IOnewayOutboundMiddleware> middleware)
    {
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));

        ArgumentNullException.ThrowIfNull(outbound);

        var terminal = new OnewayOutboundDelegate(outbound.CallAsync);
        _pipeline = MiddlewareComposer.ComposeOnewayOutbound(middleware, terminal);
    }

    /// <summary>
    /// Performs a oneway RPC with the typed request and returns the acknowledgement.
    /// </summary>
    public async ValueTask<Result<OnewayAck>> CallAsync(Request<TRequest> request, CancellationToken cancellationToken = default)
    {
        var meta = EnsureEncoding(request.Meta);

        var encodeResult = _codec.EncodeRequest(request.Body, meta);
        if (encodeResult.IsFailure)
        {
            return Err<OnewayAck>(encodeResult.Error!);
        }

        var outboundRequest = new Request<ReadOnlyMemory<byte>>(meta, encodeResult.Value);
        var ackResult = await _pipeline(outboundRequest, cancellationToken).ConfigureAwait(false);
        return ackResult;
    }

    private RequestMeta EnsureEncoding(RequestMeta meta)
    {
        ArgumentNullException.ThrowIfNull(meta);

        if (string.IsNullOrWhiteSpace(meta.Encoding))
        {
            return meta with { Encoding = _codec.Encoding };
        }

        return meta;
    }
}
