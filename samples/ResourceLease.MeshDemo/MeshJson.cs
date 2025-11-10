using System.Text.Json;

namespace OmniRelay.Samples.ResourceLease.MeshDemo;

internal static class MeshJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
}
