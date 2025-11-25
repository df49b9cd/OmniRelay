using Grpc.Net.Client;
using OmniRelay.Protos.Ca;

namespace OmniRelay.ControlPlane.Identity;

/// <summary>gRPC client for the in-process certificate authority.</summary>
public sealed class CertificateAuthorityClient : ICertificateAuthorityClient, IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly CertificateAuthority.CertificateAuthorityClient _client;

    public CertificateAuthorityClient(GrpcChannel channel)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _client = new CertificateAuthority.CertificateAuthorityClient(channel);
    }

    public Task<CertResponse> SubmitCsrAsync(CsrRequest request, CancellationToken cancellationToken = default) =>
        _client.SubmitCsrAsync(request, cancellationToken: cancellationToken).ResponseAsync;

    public Task<TrustBundleResponse> TrustBundleAsync(TrustBundleRequest request, CancellationToken cancellationToken = default) =>
        _client.TrustBundleAsync(request, cancellationToken: cancellationToken).ResponseAsync;

    public ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}
