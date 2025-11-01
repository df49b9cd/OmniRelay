using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Polymer.Core;
using Polymer.Core.Transport;
using Polymer.Errors;

namespace Polymer.Transport.Grpc;

public sealed class GrpcClientStreamingCall<TRequest, TResponse> : IAsyncDisposable
{
    private readonly RequestMeta _requestMeta;
    private readonly ICodec<TRequest, TResponse> _codec;
    private readonly AsyncClientStreamingCall<byte[], byte[]> _call;
    private readonly WriteOptions? _writeOptions;
    private readonly Task<Response<TResponse>> _responseTask;
    private bool _completed;
    private bool _disposed;

    internal GrpcClientStreamingCall(
        RequestMeta requestMeta,
        ICodec<TRequest, TResponse> codec,
        AsyncClientStreamingCall<byte[], byte[]> call,
        WriteOptions? writeOptions)
    {
        _requestMeta = requestMeta ?? throw new ArgumentNullException(nameof(requestMeta));
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        _call = call ?? throw new ArgumentNullException(nameof(call));
        _writeOptions = writeOptions;
        ResponseMeta = new ResponseMeta();

        _responseTask = ObserveResponseAsync();
    }

    public RequestMeta RequestMeta => _requestMeta;

    public ResponseMeta ResponseMeta { get; private set; }

    public Task<Response<TResponse>> Response => _responseTask;

    public async ValueTask WriteAsync(TRequest message, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(GrpcClientStreamingCall<TRequest, TResponse>));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var encodeResult = _codec.EncodeRequest(message, _requestMeta);
        if (encodeResult.IsFailure)
        {
            throw PolymerErrors.FromError(encodeResult.Error!, GrpcTransportConstants.TransportName);
        }

        try
        {
            if (_writeOptions is not null)
            {
                _call.RequestStream.WriteOptions = _writeOptions;
            }

            await _call.RequestStream.WriteAsync(encodeResult.Value).ConfigureAwait(false);
        }
        catch (RpcException rpcEx)
        {
            throw MapRpcException(rpcEx);
        }
        catch (Exception ex)
        {
            throw PolymerErrors.FromException(ex, GrpcTransportConstants.TransportName);
        }
    }

    public async ValueTask CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || _completed)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        _completed = true;

        try
        {
            await _call.RequestStream.CompleteAsync().ConfigureAwait(false);
        }
        catch (RpcException rpcEx)
        {
            throw MapRpcException(rpcEx);
        }
        catch (Exception ex)
        {
            throw PolymerErrors.FromException(ex, GrpcTransportConstants.TransportName);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            if (!_completed)
            {
                await _call.RequestStream.CompleteAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            _call.Dispose();
        }
    }

    private async Task<Response<TResponse>> ObserveResponseAsync()
    {
        try
        {
            var headers = await _call.ResponseHeadersAsync.ConfigureAwait(false);
            var payload = await _call.ResponseAsync.ConfigureAwait(false);
            var trailers = _call.GetTrailers();

            var responseMeta = GrpcMetadataAdapter.CreateResponseMeta(headers, trailers, GrpcTransportConstants.TransportName);
            ResponseMeta = responseMeta;

            var decodeResult = _codec.DecodeResponse(payload, responseMeta);
            if (decodeResult.IsFailure)
            {
                throw PolymerErrors.FromError(decodeResult.Error!, GrpcTransportConstants.TransportName);
            }

            return Response<TResponse>.Create(decodeResult.Value, responseMeta);
        }
        catch (RpcException rpcEx)
        {
            throw MapRpcException(rpcEx);
        }
        catch (Exception ex)
        {
            throw PolymerErrors.FromException(ex, GrpcTransportConstants.TransportName);
        }
    }

    private static PolymerException MapRpcException(RpcException rpcException)
    {
        var status = GrpcStatusMapper.FromStatus(rpcException.Status);
        var message = string.IsNullOrWhiteSpace(rpcException.Status.Detail)
            ? rpcException.Status.StatusCode.ToString()
            : rpcException.Status.Detail;
        var error = PolymerErrorAdapter.FromStatus(status, message, transport: GrpcTransportConstants.TransportName);
        return PolymerErrors.FromError(error, GrpcTransportConstants.TransportName);
    }
}
