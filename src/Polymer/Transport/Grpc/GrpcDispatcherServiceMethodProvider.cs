using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Polymer.Core;
using Polymer.Dispatcher;
using Polymer.Errors;

namespace Polymer.Transport.Grpc;

internal sealed class GrpcDispatcherServiceMethodProvider : IServiceMethodProvider<GrpcDispatcherService>
{
    private readonly Dispatcher.Dispatcher _dispatcher;

    public GrpcDispatcherServiceMethodProvider(Dispatcher.Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void OnServiceMethodDiscovery(ServiceMethodProviderContext<GrpcDispatcherService> context)
    {
        var unaryProcedures = _dispatcher.ListProcedures().OfType<UnaryProcedureSpec>().ToArray();
        if (unaryProcedures.Length == 0)
        {
            return;
        }

        foreach (var spec in unaryProcedures)
        {
            var method = new Method<byte[], byte[]>(
                MethodType.Unary,
                _dispatcher.ServiceName,
                spec.Name,
                GrpcMarshallerCache.ByteMarshaller,
                GrpcMarshallerCache.ByteMarshaller);

            UnaryServerMethod<GrpcDispatcherService, byte[], byte[]> handler = async (_, request, callContext) =>
            {
                var metadata = callContext.RequestHeaders ?? new Metadata();
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

            context.AddUnaryMethod<byte[], byte[]>(method, new List<object>(), handler);
        }
    }
}
