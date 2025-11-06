using Hugo;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using static Hugo.Go;

namespace OmniRelay.Core.Clients;

/// <summary>
/// Typed unary RPC client that applies middleware and uses an <see cref="ICodec{TRequest,TResponse}"/>.
/// </summary>
public sealed class UnaryClient<TRequest, TResponse>
{
    private readonly UnaryOutboundDelegate _pipeline;
    private readonly ICodec<TRequest, TResponse> _codec;

    /// <summary>
    /// Creates a unary client bound to an outbound and codec.
    /// </summary>
    public UnaryClient(IUnaryOutbound outbound, ICodec<TRequest, TResponse> codec, IReadOnlyList<IUnaryOutboundMiddleware> middleware)
    {
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        var terminal = new UnaryOutboundDelegate(outbound.CallAsync);
        _pipeline = MiddlewareComposer.ComposeUnaryOutbound(middleware, terminal);
    }

    /// <summary>
    /// Performs a unary RPC with the typed request and returns a typed response.
    /// </summary>
    public async ValueTask<Result<Response<TResponse>>> CallAsync(Request<TRequest> request, CancellationToken cancellationToken = default)
    {
        var meta = EnsureEncoding(request.Meta);

        var encodeResult = _codec.EncodeRequest(request.Body, meta);
        if (encodeResult.IsFailure)
        {
            return Err<Response<TResponse>>(encodeResult.Error!);
        }

        var rawRequest = new Request<ReadOnlyMemory<byte>>(meta, encodeResult.Value);
        var outboundResult = await _pipeline(rawRequest, cancellationToken).ConfigureAwait(false);

        if (outboundResult.IsFailure)
        {
            return Err<Response<TResponse>>(outboundResult.Error!);
        }

        var decodeResult = _codec.DecodeResponse(outboundResult.Value.Body, outboundResult.Value.Meta);
        if (decodeResult.IsFailure)
        {
            return Err<Response<TResponse>>(decodeResult.Error!);
        }

        var response = Response<TResponse>.Create(decodeResult.Value, outboundResult.Value.Meta);
        return Ok(response);
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
