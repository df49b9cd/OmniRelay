using System;
using System.Diagnostics.Metrics;
using Grpc.Core;
using Polymer.Core;

namespace Polymer.Transport.Grpc;

internal static class GrpcTransportMetrics
{
    public const string MeterName = "Polymer.Transport.Grpc";
    private static readonly Meter Meter = new(MeterName);

    public static readonly Histogram<double> ClientUnaryDuration =
        Meter.CreateHistogram<double>("polymer.grpc.client.unary.duration", unit: "ms", description: "Duration of gRPC client unary calls.");

    public static readonly Histogram<double> ServerUnaryDuration =
        Meter.CreateHistogram<double>("polymer.grpc.server.unary.duration", unit: "ms", description: "Duration of gRPC server unary calls.");

    public static readonly Histogram<double> ClientServerStreamDuration =
        Meter.CreateHistogram<double>("polymer.grpc.client.server_stream.duration", unit: "ms", description: "Duration of gRPC client server-stream calls.");

    public static readonly Histogram<double> ClientServerStreamResponseCount =
        Meter.CreateHistogram<double>("polymer.grpc.client.server_stream.responses", description: "Response message count for gRPC client server-stream calls.");

    public static readonly Counter<long> ClientServerStreamResponseMessages =
        Meter.CreateCounter<long>("polymer.grpc.client.server_stream.response_messages", description: "Total response messages observed by client server-stream calls.");

    public static readonly Histogram<double> ClientClientStreamDuration =
        Meter.CreateHistogram<double>("polymer.grpc.client.client_stream.duration", unit: "ms", description: "Duration of gRPC client streaming calls.");

    public static readonly Histogram<double> ClientClientStreamRequestCount =
        Meter.CreateHistogram<double>("polymer.grpc.client.client_stream.requests", description: "Request message count for gRPC client streaming calls.");

    public static readonly Counter<long> ClientClientStreamRequestMessages =
        Meter.CreateCounter<long>("polymer.grpc.client.client_stream.request_messages", description: "Total request messages sent by gRPC client streaming calls.");

    public static readonly Histogram<double> ClientDuplexDuration =
        Meter.CreateHistogram<double>("polymer.grpc.client.duplex.duration", unit: "ms", description: "Duration of gRPC client duplex calls.");

    public static readonly Histogram<double> ClientDuplexRequestCount =
        Meter.CreateHistogram<double>("polymer.grpc.client.duplex.requests", description: "Request message count for gRPC client duplex calls.");

    public static readonly Histogram<double> ClientDuplexResponseCount =
        Meter.CreateHistogram<double>("polymer.grpc.client.duplex.responses", description: "Response message count for gRPC client duplex calls.");

    public static readonly Counter<long> ClientDuplexRequestMessages =
        Meter.CreateCounter<long>("polymer.grpc.client.duplex.request_messages", description: "Total request messages sent by gRPC client duplex calls.");

    public static readonly Counter<long> ClientDuplexResponseMessages =
        Meter.CreateCounter<long>("polymer.grpc.client.duplex.response_messages", description: "Total response messages received by gRPC client duplex calls.");

    public static readonly Histogram<double> ServerServerStreamDuration =
        Meter.CreateHistogram<double>("polymer.grpc.server.server_stream.duration", unit: "ms", description: "Duration of gRPC server server-stream handlers.");

    public static readonly Histogram<double> ServerServerStreamResponseCount =
        Meter.CreateHistogram<double>("polymer.grpc.server.server_stream.responses", description: "Response message count for gRPC server server-stream handlers.");

    public static readonly Counter<long> ServerServerStreamResponseMessages =
        Meter.CreateCounter<long>("polymer.grpc.server.server_stream.response_messages", description: "Total response messages emitted by gRPC server server-stream handlers.");

    public static readonly Histogram<double> ServerClientStreamDuration =
        Meter.CreateHistogram<double>("polymer.grpc.server.client_stream.duration", unit: "ms", description: "Duration of gRPC server client-stream handlers.");

    public static readonly Histogram<double> ServerClientStreamRequestCount =
        Meter.CreateHistogram<double>("polymer.grpc.server.client_stream.requests", description: "Request message count for gRPC server client-stream handlers.");

    public static readonly Counter<long> ServerClientStreamRequestMessages =
        Meter.CreateCounter<long>("polymer.grpc.server.client_stream.request_messages", description: "Total request messages received by gRPC server client-stream handlers.");

    public static readonly Histogram<double> ServerClientStreamResponseCount =
        Meter.CreateHistogram<double>("polymer.grpc.server.client_stream.responses", description: "Response message count for gRPC server client-stream handlers.");

    public static readonly Counter<long> ServerClientStreamResponseMessages =
        Meter.CreateCounter<long>("polymer.grpc.server.client_stream.response_messages", description: "Total response messages emitted by gRPC server client-stream handlers.");

    public static readonly Histogram<double> ServerDuplexDuration =
        Meter.CreateHistogram<double>("polymer.grpc.server.duplex.duration", unit: "ms", description: "Duration of gRPC server duplex handlers.");

    public static readonly Histogram<double> ServerDuplexRequestCount =
        Meter.CreateHistogram<double>("polymer.grpc.server.duplex.requests", description: "Request message count for gRPC server duplex handlers.");

    public static readonly Histogram<double> ServerDuplexResponseCount =
        Meter.CreateHistogram<double>("polymer.grpc.server.duplex.responses", description: "Response message count for gRPC server duplex handlers.");

    public static readonly Counter<long> ServerDuplexRequestMessages =
        Meter.CreateCounter<long>("polymer.grpc.server.duplex.request_messages", description: "Total request messages received by gRPC server duplex handlers.");

    public static readonly Counter<long> ServerDuplexResponseMessages =
        Meter.CreateCounter<long>("polymer.grpc.server.duplex.response_messages", description: "Total response messages emitted by gRPC server duplex handlers.");

    public static KeyValuePair<string, object?>[] CreateBaseTags(RequestMeta meta)
    {
        if (meta is null)
        {
            return new[]
            {
                KeyValuePair.Create<string, object?>("rpc.system", "grpc"),
                KeyValuePair.Create<string, object?>("rpc.service", string.Empty),
                KeyValuePair.Create<string, object?>("rpc.method", string.Empty)
            };
        }

        return new[]
        {
            KeyValuePair.Create<string, object?>("rpc.system", "grpc"),
            KeyValuePair.Create<string, object?>("rpc.service", meta.Service ?? string.Empty),
            KeyValuePair.Create<string, object?>("rpc.method", meta.Procedure ?? string.Empty)
        };
    }

    public static KeyValuePair<string, object?>[] AppendStatus(KeyValuePair<string, object?>[] baseTags, StatusCode statusCode)
    {
        var tags = new KeyValuePair<string, object?>[baseTags.Length + 1];
        Array.Copy(baseTags, tags, baseTags.Length);
        tags[^1] = KeyValuePair.Create<string, object?>("rpc.grpc.status_code", statusCode);
        return tags;
    }
}
