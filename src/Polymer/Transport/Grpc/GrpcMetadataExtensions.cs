using System;
using System.Linq;
using Grpc.Core;

namespace Polymer.Transport.Grpc;

internal static class GrpcMetadataExtensions
{
    public static string? GetValue(this Metadata metadata, string key)
    {
        if (metadata is null)
        {
            return null;
        }

        return metadata.FirstOrDefault(entry => !entry.IsBinary && string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
