using Hugo;

namespace OmniRelay.Dispatcher;

/// <summary>Shared error codes for resource lease dispatcher validation.</summary>
internal static class ResourceLeaseErrors
{
    private const string PayloadRequiredCode = "resourcelease.payload.required";
    private const string ResourceTypeRequiredCode = "resourcelease.payload.resource_type_required";
    private const string ResourceIdRequiredCode = "resourcelease.payload.resource_id_required";
    private const string PartitionKeyRequiredCode = "resourcelease.payload.partition_key_required";
    private const string EncodingRequiredCode = "resourcelease.payload.encoding_required";
    private const string PendingItemRequiredCode = "resourcelease.restore.pending_item_required";

    public static Error PayloadRequired() =>
        Error.From("Resource lease payload is required.", PayloadRequiredCode);

    public static Error ResourceTypeRequired() =>
        Error.From("ResourceType is required.", ResourceTypeRequiredCode);

    public static Error ResourceIdRequired() =>
        Error.From("ResourceId is required.", ResourceIdRequiredCode);

    public static Error PartitionKeyRequired() =>
        Error.From("PartitionKey is required.", PartitionKeyRequiredCode);

    public static Error EncodingRequired() =>
        Error.From("Payload encoding is required.", EncodingRequiredCode);

    public static Error PendingItemRequired() =>
        Error.From("Pending item is required when restoring the resource lease queue.", PendingItemRequiredCode);
}
