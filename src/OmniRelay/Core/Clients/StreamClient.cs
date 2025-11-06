using System.Runtime.CompilerServices;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using OmniRelay.Errors;

namespace OmniRelay.Core.Clients;

/// <summary>
/// Typed server-streaming RPC client that applies middleware and uses an <see cref="ICodec{TRequest,TResponse}"/>.
/// </summary>
public sealed class StreamClient<TRequest, TResponse>
{
    private readonly StreamOutboundDelegate _pipeline;
    private readonly ICodec<TRequest, TResponse> _codec;

    /// <summary>
    /// Creates a server-streaming client bound to an outbound and codec.
    /// </summary>
    public StreamClient(
        IStreamOutbound outbound,
        ICodec<TRequest, TResponse> codec,
        IReadOnlyList<IStreamOutboundMiddleware> middleware)
    {
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        ArgumentNullException.ThrowIfNull(outbound);

        var terminal = new StreamOutboundDelegate(outbound.CallAsync);
        _pipeline = MiddlewareComposer.ComposeStreamOutbound(middleware, terminal);
    }

    /// <summary>
    /// Performs a server-streaming RPC and yields typed responses.
    /// </summary>
    public async IAsyncEnumerable<Response<TResponse>> CallAsync(
        Request<TRequest> request,
        StreamCallOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var meta = EnsureEncoding(request.Meta);

        var encodeResult = _codec.EncodeRequest(request.Body, meta);
        if (encodeResult.IsFailure)
        {
            throw OmniRelayErrors.FromError(encodeResult.Error!, options.Direction.ToString());
        }

        var rawRequest = new Request<ReadOnlyMemory<byte>>(meta, encodeResult.Value);
        var streamResult = await _pipeline(rawRequest, options, cancellationToken).ConfigureAwait(false);
        if (streamResult.IsFailure)
        {
            throw OmniRelayErrors.FromError(streamResult.Error!, options.Direction.ToString());
        }

        await using (streamResult.Value.AsAsyncDisposable(out var call))
        {
            await foreach (var payload in call.Responses.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                var decodeResult = _codec.DecodeResponse(payload, call.ResponseMeta);
                if (decodeResult.IsFailure)
                {
                    await call.CompleteAsync(decodeResult.Error!, cancellationToken).ConfigureAwait(false);
                    throw OmniRelayErrors.FromError(decodeResult.Error!, request.Meta.Transport ?? "stream");
                }

                yield return Response<TResponse>.Create(decodeResult.Value, call.ResponseMeta);
            }
        }
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
