using System.Text;
using System.Text.Json;

namespace OmniRelay.ControlPlane.Bootstrap;

/// <summary>HTTP client for interacting with bootstrap servers.</summary>
public sealed class BootstrapClient
{
    private readonly HttpClient _httpClient;

    public BootstrapClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<BootstrapJoinResponse> JoinAsync(Uri baseUri, BootstrapJoinRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        ArgumentNullException.ThrowIfNull(request);

        var endpoint = new Uri(baseUri, "/omnirelay/bootstrap/join");
        var requestPayload = JsonSerializer.Serialize(request, BootstrapJsonContext.Default.BootstrapJoinRequest);
        using var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"Bootstrap join failed: {(int)response.StatusCode} {response.ReasonPhrase}. Payload: {error}");
        }

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var payload = JsonSerializer.Deserialize(responseText, BootstrapJsonContext.Default.BootstrapJoinResponse)
            ?? throw new InvalidOperationException("Bootstrap server returned an empty response.");
        return payload;
    }
}
