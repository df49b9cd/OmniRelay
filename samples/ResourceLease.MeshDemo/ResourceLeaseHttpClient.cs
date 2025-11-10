using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OmniRelay.Dispatcher;

namespace OmniRelay.Samples.ResourceLease.MeshDemo;

public sealed class ResourceLeaseHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly MeshDemoOptions _options;
    private static readonly JsonSerializerOptions SerializerOptions = MeshJson.Options;

    public ResourceLeaseHttpClient(HttpClient httpClient, IOptions<MeshDemoOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options.Value;
    }

    public async Task<ResourceLeaseEnqueueResponse> EnqueueAsync(ResourceLeaseItemPayload payload, CancellationToken cancellationToken)
    {
        var request = new ResourceLeaseEnqueueRequest(payload);
        return await SendAsync<ResourceLeaseEnqueueRequest, ResourceLeaseEnqueueResponse>(
            "resourcelease.mesh::enqueue",
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResourceLeaseLeaseResponse?> LeaseAsync(string peerId, CancellationToken cancellationToken)
    {
        var request = new ResourceLeaseLeaseRequest(peerId);
        return await SendAsync<ResourceLeaseLeaseRequest, ResourceLeaseLeaseResponse>(
            "resourcelease.mesh::lease",
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task CompleteAsync(ResourceLeaseOwnershipHandle ownership, CancellationToken cancellationToken)
    {
        var request = new ResourceLeaseCompleteRequest(ownership);
        await SendAsync<ResourceLeaseCompleteRequest, ResourceLeaseAcknowledgeResponse>(
            "resourcelease.mesh::complete",
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task HeartbeatAsync(ResourceLeaseOwnershipHandle ownership, CancellationToken cancellationToken)
    {
        var request = new ResourceLeaseHeartbeatRequest(ownership);
        await SendAsync<ResourceLeaseHeartbeatRequest, ResourceLeaseAcknowledgeResponse>(
            "resourcelease.mesh::heartbeat",
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task FailAsync(ResourceLeaseOwnershipHandle ownership, bool requeue, string reason, CancellationToken cancellationToken)
    {
        var request = new ResourceLeaseFailRequest(
            ownership,
            reason,
            ErrorCode: "mesh.demo.failure",
            Requeue: requeue,
            Metadata: new Dictionary<string, string>
            {
                ["source"] = "mesh-demo"
            });

        await SendAsync<ResourceLeaseFailRequest, ResourceLeaseAcknowledgeResponse>(
            "resourcelease.mesh::fail",
            request,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> SendAsync<TRequest, TResponse>(string procedure, TRequest body, CancellationToken cancellationToken)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _httpClient.BaseAddress);
        httpRequest.Headers.TryAddWithoutValidation("Rpc-Procedure", procedure);
        httpRequest.Headers.TryAddWithoutValidation("Rpc-Service", _options.ServiceName);
        httpRequest.Headers.TryAddWithoutValidation("Rpc-Encoding", "application/json");
        httpRequest.Headers.TryAddWithoutValidation("Rpc-Caller", "mesh-demo-host");
        httpRequest.Content = JsonContent.Create(body, options: SerializerOptions);

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync<TResponse>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException($"RPC '{procedure}' returned no payload.");
    }
}
