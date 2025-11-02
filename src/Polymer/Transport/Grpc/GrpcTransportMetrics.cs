using System.Diagnostics.Metrics;

namespace Polymer.Transport.Grpc;

internal static class GrpcTransportMetrics
{
    public const string MeterName = "Polymer.Transport.Grpc";
    private static readonly Meter Meter = new(MeterName);

    public static readonly Histogram<double> ClientUnaryDuration =
        Meter.CreateHistogram<double>("polymer.grpc.client.unary.duration", unit: "ms", description: "Duration of gRPC client unary calls.");

    public static readonly Histogram<double> ServerUnaryDuration =
        Meter.CreateHistogram<double>("polymer.grpc.server.unary.duration", unit: "ms", description: "Duration of gRPC server unary calls.");

    public static readonly Histogram<double> ClientStreamDuration =
        Meter.CreateHistogram<double>("polymer.grpc.client.stream.duration", unit: "ms", description: "Duration of gRPC client server-stream calls.");

    public static readonly Histogram<double> ClientStreamMessageCount =
        Meter.CreateHistogram<double>("polymer.grpc.client.stream.messages", description: "Message count for gRPC client streaming responses.");

    public static readonly Histogram<double> ServerUnaryMessageSize =
        Meter.CreateHistogram<double>("polymer.grpc.server.unary.request_size", unit: "bytes", description: "Request payload size for gRPC unary calls.");
}
