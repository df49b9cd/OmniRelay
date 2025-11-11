using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniRelay.Core.Gossip;

/// <summary>Source-generated serializer context for gossip payloads.</summary>
[JsonSerializable(typeof(MeshGossipEnvelope))]
[JsonSerializable(typeof(MeshGossipMemberSnapshot))]
[JsonSerializable(typeof(MeshGossipClusterView))]
internal partial class MeshGossipJsonSerializerContext : JsonSerializerContext
{
    public static MeshGossipJsonSerializerContext Default { get; } =
        new(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        });
}
