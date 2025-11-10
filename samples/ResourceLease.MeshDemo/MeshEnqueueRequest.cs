using System.Text;
using System.Text.Json;
using OmniRelay.Dispatcher;

namespace OmniRelay.Samples.ResourceLease.MeshDemo;

public sealed record MeshEnqueueRequest(
    string ResourceType,
    string ResourceId,
    string PartitionKey = "default",
    Dictionary<string, string>? Attributes = null,
    string? Body = null,
    string? RequestId = null)
{
    public ResourceLeaseItemPayload ToPayload()
    {
        var effectiveBody = string.IsNullOrWhiteSpace(Body)
            ? JsonSerializer.SerializeToUtf8Bytes(
                new MeshEnqueuePayloadBody($"work:{ResourceId}", DateTimeOffset.UtcNow),
                MeshJson.Context.MeshEnqueuePayloadBody)
            : Encoding.UTF8.GetBytes(Body!);

        return new ResourceLeaseItemPayload(
            ResourceType,
            ResourceId,
            PartitionKey,
            PayloadEncoding: "application/json",
            Body: effectiveBody,
            Attributes,
            RequestId ?? Guid.NewGuid().ToString("N"));
    }
}

internal sealed record MeshEnqueuePayloadBody(string Message, DateTimeOffset CreatedAt);
