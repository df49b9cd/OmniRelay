using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OmniRelay.Diagnostics.Alerting;

internal sealed class WebhookAlertChannel : IAlertChannel
{
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;
    private readonly IReadOnlyDictionary<string, string> _headers;
    private readonly string? _authenticationToken;

    public WebhookAlertChannel(string name, Uri endpoint, HttpClient httpClient, IReadOnlyDictionary<string, string> headers, string? authenticationToken)
    {
        Name = name;
        _endpoint = endpoint;
        _httpClient = httpClient;
        _headers = headers;
        _authenticationToken = authenticationToken;
    }

    public string Name { get; }

    public async ValueTask SendAsync(AlertEvent alert, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        foreach (var header in _headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (!string.IsNullOrWhiteSpace(_authenticationToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authenticationToken);
        }

        var payload = new
        {
            alert.Name,
            alert.Severity,
            alert.Message,
            metadata = alert.Metadata
        };

        request.Content = JsonContent.Create(payload);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
