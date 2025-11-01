using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Polymer.Core;
using Polymer.Core.Transport;
using Polymer.Dispatcher;
using Polymer.Errors;

namespace Polymer.Transport.Grpc;

internal sealed class GrpcDispatcherServiceMethodProvider(Dispatcher.Dispatcher dispatcher) : IServiceMethodProvider<GrpcDispatcherService>
{
    private readonly Dispatcher.Dispatcher _dispatcher = dispatcher;

    public void OnServiceMethodDiscovery(ServiceMethodProviderContext<GrpcDispatcherService> context)
    {
        var procedures = _dispatcher.ListProcedures();

        foreach (var spec in procedures.OfType<UnaryProcedureSpec>())
        {
            var method = new Method<byte[], byte[]>(
                MethodType.Unary,
                _dispatcher.ServiceName,
                spec.Name,
                GrpcMarshallerCache.ByteMarshaller,
                GrpcMarshallerCache.ByteMarshaller);

            UnaryServerMethod<GrpcDispatcherService, byte[], byte[]> handler = async (_, request, callContext) =>
            {
                var metadata = callContext.RequestHeaders ?? [];
                var encoding = metadata.GetValue(GrpcTransportConstants.EncodingHeader);

                var requestMeta = GrpcMetadataAdapter.BuildRequestMeta(
                    _dispatcher.ServiceName,
                    spec.Name,
                    metadata,
                    encoding);

                var dispatcherRequest = new Request<ReadOnlyMemory<byte>>(requestMeta, request);
                var result = await _dispatcher.InvokeUnaryAsync(spec.Name, dispatcherRequest, callContext.CancellationToken)
                    .ConfigureAwait(false);

                if (result.IsFailure)
                {
                    var exception = PolymerErrors.FromError(result.Error!, GrpcTransportConstants.TransportName);
                    var status = GrpcStatusMapper.ToStatus(exception.StatusCode, exception.Message);
                    var trailers = GrpcMetadataAdapter.CreateErrorTrailers(exception.Error);
                    throw new RpcException(status, trailers);
                }

                var response = result.Value;
                var headers = GrpcMetadataAdapter.CreateResponseHeaders(response.Meta);
                if (headers.Count > 0)
                {
                    await callContext.WriteResponseHeadersAsync(headers).ConfigureAwait(false);
                }

                return response.Body.ToArray();
            };

            context.AddUnaryMethod<byte[], byte[]>(method, [], handler);
        }

        foreach (var spec in procedures.OfType<OnewayProcedureSpec>())
        {
            var method = new Method<byte[], byte[]>(
                MethodType.Unary,
                _dispatcher.ServiceName,
                spec.Name,
                GrpcMarshallerCache.ByteMarshaller,
                GrpcMarshallerCache.ByteMarshaller);

            UnaryServerMethod<GrpcDispatcherService, byte[], byte[]> handler = async (_, request, callContext) =>
            {
                var metadata = callContext.RequestHeaders ?? [];
                var encoding = metadata.GetValue(GrpcTransportConstants.EncodingHeader);

                var requestMeta = GrpcMetadataAdapter.BuildRequestMeta(
                    _dispatcher.ServiceName,
                    spec.Name,
                    metadata,
                    encoding);

                var dispatcherRequest = new Request<ReadOnlyMemory<byte>>(requestMeta, request);
                var result = await _dispatcher.InvokeOnewayAsync(spec.Name, dispatcherRequest, callContext.CancellationToken)
                    .ConfigureAwait(false);

                if (result.IsFailure)
                {
                    var exception = PolymerErrors.FromError(result.Error!, GrpcTransportConstants.TransportName);
                    var status = GrpcStatusMapper.ToStatus(exception.StatusCode, exception.Message);
                    var trailers = GrpcMetadataAdapter.CreateErrorTrailers(exception.Error);
                    throw new RpcException(status, trailers);
                }

                var headers = GrpcMetadataAdapter.CreateResponseHeaders(result.Value.Meta);
                if (headers.Count > 0)
                {
                    await callContext.WriteResponseHeadersAsync(headers).ConfigureAwait(false);
                }

                return [];
            };

            context.AddUnaryMethod<byte[], byte[]>(method, [], handler);
        }

        foreach (var spec in procedures.OfType<StreamProcedureSpec>())
        {
            var method = new Method<byte[], byte[]>(
                MethodType.ServerStreaming,
                _dispatcher.ServiceName,
                spec.Name,
                GrpcMarshallerCache.ByteMarshaller,
                GrpcMarshallerCache.ByteMarshaller);

            ServerStreamingServerMethod<GrpcDispatcherService, byte[], byte[]> handler = async (_, request, responseStream, callContext) =>
            {
                var metadata = callContext.RequestHeaders ?? [];
                var encoding = metadata.GetValue(GrpcTransportConstants.EncodingHeader);

                var requestMeta = GrpcMetadataAdapter.BuildRequestMeta(
                    _dispatcher.ServiceName,
                    spec.Name,
                    metadata,
                    encoding);

                var dispatcherRequest = new Request<ReadOnlyMemory<byte>>(requestMeta, request);
                var streamResult = await _dispatcher.InvokeStreamAsync(
                    spec.Name,
                    dispatcherRequest,
                    new StreamCallOptions(StreamDirection.Server),
                    callContext.CancellationToken).ConfigureAwait(false);

                if (streamResult.IsFailure)
                {
                    var exception = PolymerErrors.FromError(streamResult.Error!, GrpcTransportConstants.TransportName);
                    var status = GrpcStatusMapper.ToStatus(exception.StatusCode, exception.Message);
                    var trailers = GrpcMetadataAdapter.CreateErrorTrailers(exception.Error);
                    throw new RpcException(status, trailers);
                }

                await using var streamCall = streamResult.Value;

                var headers = GrpcMetadataAdapter.CreateResponseHeaders(streamCall.ResponseMeta);
                if (headers.Count > 0)
                {
                    await callContext.WriteResponseHeadersAsync(headers).ConfigureAwait(false);
                }

                await foreach (var payload in streamCall.Responses.ReadAllAsync(callContext.CancellationToken).ConfigureAwait(false))
                {
                    await responseStream.WriteAsync(payload.ToArray()).ConfigureAwait(false);
                }
            };

            context.AddServerStreamingMethod<byte[], byte[]>(method, [], handler);
        }

        foreach (var spec in procedures.OfType<ClientStreamProcedureSpec>())
        {
            var method = new Method<byte[], byte[]>(
                MethodType.ClientStreaming,
                _dispatcher.ServiceName,
                spec.Name,
                GrpcMarshallerCache.ByteMarshaller,
                GrpcMarshallerCache.ByteMarshaller);

            ClientStreamingServerMethod<GrpcDispatcherService, byte[], byte[]> handler = async (_, requestStream, callContext) =>
            {
                var metadata = callContext.RequestHeaders ?? [];
                var encoding = metadata.GetValue(GrpcTransportConstants.EncodingHeader);

                var requestMeta = GrpcMetadataAdapter.BuildRequestMeta(
                    _dispatcher.ServiceName,
                    spec.Name,
                    metadata,
                    encoding);

                if (callContext.Deadline != DateTime.MaxValue)
                {
                    var deadlineUtc = DateTime.SpecifyKind(callContext.Deadline, DateTimeKind.Utc);
                    requestMeta = requestMeta with { Deadline = new DateTimeOffset(deadlineUtc) };
                }

                var callResult = await _dispatcher.InvokeClientStreamAsync(
                    spec.Name,
                    requestMeta,
                    callContext.CancellationToken).ConfigureAwait(false);

                if (callResult.IsFailure)
                {
                    var exception = PolymerErrors.FromError(callResult.Error!, GrpcTransportConstants.TransportName);
                    var status = GrpcStatusMapper.ToStatus(exception.StatusCode, exception.Message);
                    var trailers = GrpcMetadataAdapter.CreateErrorTrailers(exception.Error);
                    throw new RpcException(status, trailers);
                }

                await using var clientStreamCall = callResult.Value;
                var cancellationToken = callContext.CancellationToken;

                try
                {
                    while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false))
                    {
                        var payload = requestStream.Current;
                        if (payload is null)
                        {
                            continue;
                        }

                        await clientStreamCall.Requests.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                    }

                    await clientStreamCall.CompleteWriterAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (RpcException rpcEx)
                {
                    var status = GrpcStatusMapper.FromStatus(rpcEx.Status);
                    var message = string.IsNullOrWhiteSpace(rpcEx.Status.Detail)
                        ? rpcEx.Status.StatusCode.ToString()
                        : rpcEx.Status.Detail;
                    var error = PolymerErrorAdapter.FromStatus(status, message, transport: GrpcTransportConstants.TransportName);
                    await clientStreamCall.CompleteWriterAsync(error).ConfigureAwait(false);
                    throw;
                }
                catch (OperationCanceledException)
                {
                    var error = PolymerErrorAdapter.FromStatus(
                        PolymerStatusCode.Cancelled,
                        "The client cancelled the request.",
                        transport: GrpcTransportConstants.TransportName);
                    await clientStreamCall.CompleteWriterAsync(error).ConfigureAwait(false);
                    throw new RpcException(GrpcStatusMapper.ToStatus(PolymerStatusCode.Cancelled, "The client cancelled the request."));
                }
                catch (Exception ex)
                {
                    var message = string.IsNullOrWhiteSpace(ex.Message)
                        ? "An error occurred while reading the client stream."
                        : ex.Message;
                    var error = PolymerErrorAdapter.FromStatus(
                        PolymerStatusCode.Internal,
                        message,
                        transport: GrpcTransportConstants.TransportName);
                    await clientStreamCall.CompleteWriterAsync(error).ConfigureAwait(false);
                    throw new RpcException(GrpcStatusMapper.ToStatus(PolymerStatusCode.Internal, message));
                }

                var responseResult = await clientStreamCall.Response.ConfigureAwait(false);

                if (responseResult.IsFailure)
                {
                    var exception = PolymerErrors.FromError(responseResult.Error!, GrpcTransportConstants.TransportName);
                    var status = GrpcStatusMapper.ToStatus(exception.StatusCode, exception.Message);
                    var trailers = GrpcMetadataAdapter.CreateErrorTrailers(exception.Error);
                    throw new RpcException(status, trailers);
                }

                var response = responseResult.Value;
                var headers = GrpcMetadataAdapter.CreateResponseHeaders(response.Meta);
                if (headers.Count > 0)
                {
                    await callContext.WriteResponseHeadersAsync(headers).ConfigureAwait(false);
                }

                return response.Body.ToArray();
            };

            context.AddClientStreamingMethod<byte[], byte[]>(method, [], handler);
        }
    }
}
