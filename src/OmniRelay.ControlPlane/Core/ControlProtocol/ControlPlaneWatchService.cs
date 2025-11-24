using Google.Protobuf;
using Grpc.Core;
using OmniRelay.Protos.Control;

namespace OmniRelay.ControlPlane.ControlProtocol;

/// <summary>
/// Control-plane watch service (delta + snapshot). Placeholder implementation; to be wired to real config sources.
/// </summary>
public sealed class ControlPlaneWatchService : ControlPlaneWatch.ControlPlaneWatchBase
{
    private const string CurrentVersion = "1";
    private const long CurrentEpoch = 1;
    private static readonly byte[] CurrentPayload = Array.Empty<byte>();
    private static readonly string[] SupportedCapabilities = ["core/v1", "dsl/v1"];
    private static readonly ControlBackoff DefaultBackoff = new() { Millis = 1000 };

    public override Task<ControlSnapshotResponse> Snapshot(ControlSnapshotRequest request, ServerCallContext context)
    {
        var unsupported = GetUnsupported(request.Capabilities);
        if (unsupported.Count > 0)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"unsupported capabilities: {string.Join(',', unsupported)}"));
        }

        var response = new ControlSnapshotResponse
        {
            Version = CurrentVersion,
            Epoch = CurrentEpoch,
            Payload = ByteString.CopyFrom(CurrentPayload)
        };
        response.RequiredCapabilities.AddRange(SupportedCapabilities);
        return Task.FromResult(response);
    }

    public override async Task Watch(ControlWatchRequest request, IServerStreamWriter<ControlWatchResponse> responseStream, ServerCallContext context)
    {
        var unsupported = GetUnsupported(request.Capabilities);
        if (unsupported.Count > 0)
        {
            await responseStream.WriteAsync(new ControlWatchResponse
            {
                Error = new ControlError
                {
                    Code = "unsupported_capability",
                    Message = $"Capabilities not supported: {string.Join(',', unsupported)}",
                    Remediation = "Update agent or disable unsupported features."
                },
                Backoff = new ControlBackoff { Millis = 5000 }
            }).ConfigureAwait(false);
            return;
        }

        var resumeVersion = request.ResumeToken?.Version ?? string.Empty;
        var fullSnapshot = !string.Equals(resumeVersion, CurrentVersion, StringComparison.OrdinalIgnoreCase);

        var response = new ControlWatchResponse
        {
            Version = CurrentVersion,
            Epoch = CurrentEpoch,
            Payload = ByteString.CopyFrom(CurrentPayload),
            FullSnapshot = fullSnapshot,
            ResumeToken = new WatchResumeToken
            {
                Version = CurrentVersion,
                Epoch = CurrentEpoch,
                Opaque = ByteString.CopyFromUtf8(request.NodeId ?? string.Empty)
            },
            Backoff = DefaultBackoff
        };

        response.RequiredCapabilities.AddRange(SupportedCapabilities);
        await responseStream.WriteAsync(response).ConfigureAwait(false);
    }

    private static List<string> GetUnsupported(CapabilitySet? capabilitySet)
    {
        if (capabilitySet?.Items is null || capabilitySet.Items.Count == 0)
        {
            return new List<string>();
        }

        var list = new List<string>();
        foreach (var cap in capabilitySet.Items)
        {
            if (Array.IndexOf(SupportedCapabilities, cap) < 0)
            {
                list.Add(cap);
            }
        }

        return list;
    }
}
