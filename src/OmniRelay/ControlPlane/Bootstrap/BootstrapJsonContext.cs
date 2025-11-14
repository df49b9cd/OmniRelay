using System.Text.Json.Serialization;

namespace OmniRelay.ControlPlane.Bootstrap;

[JsonSerializable(typeof(BootstrapJoinRequest))]
[JsonSerializable(typeof(BootstrapJoinResponse))]
internal partial class BootstrapJsonContext : JsonSerializerContext
{
}
