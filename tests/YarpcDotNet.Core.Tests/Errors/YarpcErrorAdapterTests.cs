using System.Collections.Generic;
using Hugo;
using Xunit;
using YarpcDotNet.Core.Errors;

namespace YarpcDotNet.Core.Tests.Errors;

public class YarpcErrorAdapterTests
{
    [Fact]
    public void FromStatus_AttachesMetadata()
    {
        var error = YarpcErrorAdapter.FromStatus(
            YarpcStatusCode.PermissionDenied,
            "denied",
            transport: "grpc");

        Assert.Equal("permission-denied", error.Code);
        Assert.True(error.TryGetMetadata("yarpc.status", out string? status));
        Assert.Equal(nameof(YarpcStatusCode.PermissionDenied), status);
        Assert.True(error.TryGetMetadata("yarpc.transport", out string? transport));
        Assert.Equal("grpc", transport);
    }

    [Fact]
    public void FromStatus_MergesAdditionalMetadata()
    {
        var error = YarpcErrorAdapter.FromStatus(
            YarpcStatusCode.ResourceExhausted,
            "busy",
            metadata: new Dictionary<string, object?>
            {
                { "retryable", true },
                { "node", "alpha" }
            });

        Assert.True(error.TryGetMetadata("retryable", out bool retryable));
        Assert.True(retryable);
        Assert.True(error.TryGetMetadata("node", out string? node));
        Assert.Equal("alpha", node);
    }

    [Fact]
    public void ToStatus_UsesMetadataPriority()
    {
        var error = Error.From("denied")
            .WithMetadata("yarpc.status", nameof(YarpcStatusCode.Unavailable));

        var status = YarpcErrorAdapter.ToStatus(error);

        Assert.Equal(YarpcStatusCode.Unavailable, status);
    }

    [Fact]
    public void ToStatus_FallsBackToCode()
    {
        var error = Error.From("internal failure", "internal");

        var status = YarpcErrorAdapter.ToStatus(error);

        Assert.Equal(YarpcStatusCode.Internal, status);
    }

    [Fact]
    public void ToStatus_MapsCancellationCause()
    {
        var error = Error.From("cancelled").WithCause(new OperationCanceledException());

        var status = YarpcErrorAdapter.ToStatus(error);

        Assert.Equal(YarpcStatusCode.Cancelled, status);
    }
}
