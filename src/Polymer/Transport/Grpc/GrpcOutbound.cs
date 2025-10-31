using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Hugo;
using Polymer.Core;
using Polymer.Core.Transport;
using Polymer.Errors;
using static Hugo.Go;

namespace Polymer.Transport.Grpc;

public sealed class GrpcOutbound : IUnaryOutbound, IOnewayOutbound
{
    private readonly Uri _address;
    private readonly string _remoteService;
    private readonly GrpcChannelOptions _channelOptions;
    private GrpcChannel? _channel;
    private CallInvoker? _callInvoker;
    private readonly ConcurrentDictionary<string, Method<byte[], byte[]>> _methodCache = new();

    public GrpcOutbound(Uri address, string remoteService, GrpcChannelOptions? channelOptions = null)
    {
        _address = address ?? throw new ArgumentNullException(nameof(address));
        _remoteService = string.IsNullOrWhiteSpace(remoteService)
            ? throw new ArgumentException("Remote service name must be provided.", nameof(remoteService))
            : remoteService;
        _channelOptions = channelOptions ?? new GrpcChannelOptions
        {
            HttpHandler = new System.Net.Http.SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            }
        };
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _channel = GrpcChannel.ForAddress(_address, _channelOptions);
        _callInvoker = _channel.CreateCallInvoker();
        return ValueTask.CompletedTask;
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_channel is not null)
        {
            _channel.Dispose();
            _channel = null;
            _callInvoker = null;
            _methodCache.Clear();
        }
    }

    public async ValueTask<Result<Response<ReadOnlyMemory<byte>>>> CallAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken = default)
    {
        if (_callInvoker is null)
        {
            throw new InvalidOperationException("gRPC outbound has not been started.");
        }

        if (string.IsNullOrEmpty(request.Meta.Procedure))
        {
            return Err<Response<ReadOnlyMemory<byte>>>(
                PolymerErrorAdapter.FromStatus(PolymerStatusCode.InvalidArgument, "Procedure metadata is required for gRPC calls.", transport: GrpcTransportConstants.TransportName));
        }

        var method = _methodCache.GetOrAdd(request.Meta.Procedure, CreateMethod);
        var metadata = GrpcMetadataAdapter.CreateRequestMetadata(request.Meta);
        var callOptions = new CallOptions(metadata, cancellationToken: cancellationToken);

        try
        {
            var call = _callInvoker.AsyncUnaryCall(method, null, callOptions, request.Body.ToArray());
            var response = await call.ResponseAsync.ConfigureAwait(false);

            var headers = await call.ResponseHeadersAsync.ConfigureAwait(false);
            var trailers = call.GetTrailers();

            var responseMeta = GrpcMetadataAdapter.CreateResponseMeta(headers, trailers);

            return Ok(Response<ReadOnlyMemory<byte>>.Create(response, responseMeta));
        }
        catch (RpcException rpcEx)
        {
            var status = GrpcStatusMapper.FromStatus(rpcEx.Status);
            var message = string.IsNullOrWhiteSpace(rpcEx.Status.Detail) ? rpcEx.Status.StatusCode.ToString() : rpcEx.Status.Detail;
            var error = PolymerErrorAdapter.FromStatus(status, message, transport: GrpcTransportConstants.TransportName);
            return Err<Response<ReadOnlyMemory<byte>>>(error);
        }
        catch (Exception ex)
        {
            return PolymerErrors.ToResult<Response<ReadOnlyMemory<byte>>>(ex, transport: GrpcTransportConstants.TransportName);
        }
    }

    private Method<byte[], byte[]> CreateMethod(string procedure)
    {
        return new Method<byte[], byte[]>(
            MethodType.Unary,
            _remoteService,
            procedure,
            GrpcMarshallerCache.ByteMarshaller,
            GrpcMarshallerCache.ByteMarshaller);
    }

    async ValueTask<Result<OnewayAck>> IOnewayOutbound.CallAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken)
    {
        if (_callInvoker is null)
        {
            throw new InvalidOperationException("gRPC outbound has not been started.");
        }

        if (string.IsNullOrEmpty(request.Meta.Procedure))
        {
            return Err<OnewayAck>(
                PolymerErrorAdapter.FromStatus(PolymerStatusCode.InvalidArgument, "Procedure metadata is required for gRPC calls.", transport: GrpcTransportConstants.TransportName));
        }

        var method = _methodCache.GetOrAdd(request.Meta.Procedure, CreateMethod);
        var metadata = GrpcMetadataAdapter.CreateRequestMetadata(request.Meta);
        var callOptions = new CallOptions(metadata, cancellationToken: cancellationToken);

        try
        {
            var call = _callInvoker.AsyncUnaryCall(method, null, callOptions, request.Body.ToArray());
            await call.ResponseAsync.ConfigureAwait(false);

            var headers = await call.ResponseHeadersAsync.ConfigureAwait(false);
            var trailers = call.GetTrailers();
            var responseMeta = GrpcMetadataAdapter.CreateResponseMeta(headers, trailers);

            return Ok(OnewayAck.Ack(responseMeta));
        }
        catch (RpcException rpcEx)
        {
            var status = GrpcStatusMapper.FromStatus(rpcEx.Status);
            var message = string.IsNullOrWhiteSpace(rpcEx.Status.Detail) ? rpcEx.Status.StatusCode.ToString() : rpcEx.Status.Detail;
            var error = PolymerErrorAdapter.FromStatus(status, message, transport: GrpcTransportConstants.TransportName);
            return Err<OnewayAck>(error);
        }
        catch (Exception ex)
        {
            return PolymerErrors.ToResult<OnewayAck>(ex, transport: GrpcTransportConstants.TransportName);
        }
    }
}
