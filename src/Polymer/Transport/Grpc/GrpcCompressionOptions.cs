using System;
using System.Collections.Generic;
using System.IO.Compression;
using Grpc.Net.Compression;

namespace Polymer.Transport.Grpc;

public sealed record GrpcCompressionOptions
{
    public IReadOnlyList<ICompressionProvider> Providers { get; init; } = Array.Empty<ICompressionProvider>();

    public string? DefaultAlgorithm { get; init; }

    public CompressionLevel? DefaultCompressionLevel { get; init; }
}
