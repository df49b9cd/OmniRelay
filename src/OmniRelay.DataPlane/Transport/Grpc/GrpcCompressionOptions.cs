using System.IO.Compression;
using Grpc.Net.Compression;
using Hugo;
using static Hugo.Go;
using OmniRelay.Errors;

namespace OmniRelay.Transport.Grpc;

/// <summary>
/// Compression options for gRPC transport including providers and defaults.
/// </summary>
public sealed record GrpcCompressionOptions
{
    public IReadOnlyList<ICompressionProvider> Providers { get; init; } = [];

    public string? DefaultAlgorithm { get; init; }

    public CompressionLevel? DefaultCompressionLevel { get; init; }

    /// <summary>
/// Validates that the configured default algorithm has a corresponding registered provider.
/// </summary>
public Result<Unit> Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultAlgorithm))
        {
            return Ok(Unit.Value);
        }

        if (Providers is null)
        {
            return Err<Unit>(OmniRelayErrorAdapter.FromStatus(
                OmniRelayStatusCode.InvalidArgument,
                $"Compression options specify default algorithm '{DefaultAlgorithm}' but no providers are registered.",
                transport: GrpcTransportConstants.TransportName));
        }

        foreach (var provider in Providers)
        {
            if (provider is null)
            {
                continue;
            }

            if (string.Equals(provider.EncodingName, DefaultAlgorithm, StringComparison.OrdinalIgnoreCase))
            {
                return Ok(Unit.Value);
            }
        }

        return Err<Unit>(OmniRelayErrorAdapter.FromStatus(
            OmniRelayStatusCode.InvalidArgument,
            $"Compression provider for algorithm '{DefaultAlgorithm}' was not registered.",
            transport: GrpcTransportConstants.TransportName));
    }
}
