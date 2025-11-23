using System.Runtime.CompilerServices;
using Hugo;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using OmniRelay.Errors;
using static Hugo.Go;

namespace OmniRelay.Core.Clients;

/// <summary>
/// Typed server-streaming RPC client that applies middleware and uses an <see cref="ICodec{TRequest,TResponse}"/>.
/// </summary>
public sealed class StreamClient<TRequest, TResponse>
{
    private readonly StreamOutboundHandler _pipeline;
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
        ArgumentNullException.ThrowIfNull(middleware);

        var terminal = new StreamOutboundHandler(outbound.CallAsync);
        _pipeline = MiddlewareComposer.ComposeStreamOutbound(middleware, terminal);
    }

    /// <summary>
    /// Performs a server-streaming RPC and yields result-wrapped typed responses.
    /// </summary>
    public async IAsyncEnumerable<Result<Response<TResponse>>> CallAsync(
        Request<TRequest> request,
        StreamCallOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var meta = EnsureEncoding(request.Meta);
        var transport = request.Meta.Transport ?? options.Direction.ToString();

        var streamResult = await _codec.EncodeRequest(request.Body, meta)
            .Map(payload => new Request<ReadOnlyMemory<byte>>(meta, payload))
            .ThenAsync(
                (outboundRequest, token) => _pipeline(outboundRequest, options, token),
                cancellationToken)
            .ConfigureAwait(false);
        if (streamResult.IsFailure)
        {
            yield return OmniRelayErrors.ToResult<Response<TResponse>>(streamResult.Error!, transport);
            yield break;
        }

        await using var callLease = streamResult.Value.AsAsyncDisposable(out var call).ConfigureAwait(false);
        var responseStream = Result.MapStreamAsync(
                call.Responses.ReadAllAsync(cancellationToken),
                (payload, token) => new ValueTask<Result<Response<TResponse>>>(
                    DecodeResponse(payload, call.ResponseMeta, transport)),
                cancellationToken);

        await foreach (var result in responseStream.ConfigureAwait(false))
        {
            if (result.IsFailure && result.Error is { } error)
            {
                await call.CompleteAsync(error, cancellationToken).ConfigureAwait(false);
                yield return result;
                yield break;
            }

            yield return result;
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

    private Result<Response<TResponse>> DecodeResponse(
        ReadOnlyMemory<byte> payload,
        ResponseMeta meta,
        string transport)
    {
        var decoded = _codec.DecodeResponse(payload, meta);
        return decoded.IsSuccess
            ? Ok(Response<TResponse>.Create(decoded.Value, meta))
            : OmniRelayErrors.ToResult<Response<TResponse>>(decoded.Error!, transport);
    }
}
