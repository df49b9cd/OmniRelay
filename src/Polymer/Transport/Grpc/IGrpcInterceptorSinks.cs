using Polymer.Transport.Grpc.Interceptors;

namespace Polymer.Transport.Grpc;

internal interface IGrpcClientInterceptorSink
{
    void AttachGrpcClientInterceptors(string service, GrpcClientInterceptorRegistry registry);
}

internal interface IGrpcServerInterceptorSink
{
    void AttachGrpcServerInterceptors(GrpcServerInterceptorRegistry registry);
}
