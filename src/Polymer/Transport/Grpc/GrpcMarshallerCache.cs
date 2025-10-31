using Grpc.Core;

namespace Polymer.Transport.Grpc;

internal static class GrpcMarshallerCache
{
    public static readonly Marshaller<byte[]> ByteMarshaller = Marshallers.Create(
        serializer: payload => payload ?? System.Array.Empty<byte>(),
        deserializer: payload => payload ?? System.Array.Empty<byte>());
}
