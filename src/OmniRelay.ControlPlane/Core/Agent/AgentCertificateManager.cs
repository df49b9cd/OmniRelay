using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Hugo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniRelay.ControlPlane.Identity;
using OmniRelay.Core.Transport;
using OmniRelay.Protos.Ca;
using static Hugo.Go;

namespace OmniRelay.ControlPlane.Agent;

/// <summary>Handles mTLS certificate issuance and renewal for the local agent.</summary>
public sealed class AgentCertificateManager : ILifecycle, IDisposable
{
    private readonly ICertificateAuthorityClient _caClient;
    private readonly MeshAgentOptions _options;
    private readonly AgentCertificateOptions _certOptions;
    private readonly ILogger<AgentCertificateManager> _logger;
    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private bool _disposed;

    public AgentCertificateManager(
        ICertificateAuthorityClient caClient,
        IOptions<MeshAgentOptions> options,
        ILogger<AgentCertificateManager> logger,
        TimeProvider? timeProvider = null)
    {
        _caClient = caClient ?? throw new ArgumentNullException(nameof(caClient));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _certOptions = _options.Certificates ?? new AgentCertificateOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || !_certOptions.Enabled || _loop is not null)
        {
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cts.Token;
        _loop = Go.Run(ct => RunAsync(ct), cancellationToken: token).AsTask();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        var cts = Interlocked.Exchange(ref _cts, null);
        var loop = Interlocked.Exchange(ref _loop, null);
        cts?.Cancel();

        if (loop is not null)
        {
            try
            {
                await loop.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        cts?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>Executes a single renewal check (public for tests).</summary>
    public ValueTask<Result<CertificateRenewalPlan>> EnsureCurrentAsync(CancellationToken cancellationToken = default) =>
        EnsureCurrentInternalAsync(cancellationToken);

    private async ValueTask RunAsync(CancellationToken cancellationToken)
    {
        var backoff = _certOptions.FailureBackoff;
        while (!cancellationToken.IsCancellationRequested)
        {
            var plan = await EnsureCurrentInternalAsync(cancellationToken).ConfigureAwait(false);
            if (plan.IsSuccess)
            {
                backoff = _certOptions.FailureBackoff;
                AgentLog.AgentCertificateNextCheck(_logger, (long)plan.Value.NextCheck.TotalMilliseconds);

                try
                {
                    await Task.Delay(plan.Value.NextCheck, _timeProvider, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                continue;
            }

            AgentLog.AgentCertificateRenewalFailed(_logger, plan.Error?.Message ?? "unknown");
            try
            {
                await Task.Delay(backoff, _timeProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            backoff = IncreaseBackoff(backoff);
        }
    }

    private async ValueTask<Result<CertificateRenewalPlan>> EnsureCurrentInternalAsync(CancellationToken cancellationToken)
    {
        var existing = TryLoadExisting();
        if (existing.IsFailure)
        {
            return existing.CastFailure<CertificateRenewalPlan>();
        }

        var now = _timeProvider.GetUtcNow();
        if (existing.Value is { } state && now < state.RenewAfter)
        {
            var wait = state.RenewAfter - now;
            if (wait < _certOptions.MinRenewalInterval)
            {
                wait = _certOptions.MinRenewalInterval;
            }

            return Ok(new CertificateRenewalPlan(wait, false));
        }

        var renewed = await RenewAsync(cancellationToken).ConfigureAwait(false);
        if (renewed.IsFailure)
        {
            return renewed.CastFailure<CertificateRenewalPlan>();
        }

        var delay = renewed.Value.RenewAfter - now;
        if (delay < _certOptions.MinRenewalInterval)
        {
            delay = _certOptions.MinRenewalInterval;
        }

        AgentLog.AgentCertificateRenewed(_logger, renewed.Value.ExpiresAt);
        return Ok(new CertificateRenewalPlan(delay, true));
    }

    private Result<CertificateState?> TryLoadExisting()
    {
        if (!File.Exists(_certOptions.PfxPath))
        {
            return Ok<CertificateState?>(null);
        }

        try
        {
            using var cert = X509CertificateLoader.LoadPkcs12FromFile(
                _certOptions.PfxPath,
                _certOptions.PfxPassword,
                X509KeyStorageFlags.Exportable);

            var renewAfter = CalculateRenewAfter(cert.NotBefore, cert.NotAfter);
            return Ok<CertificateState?>(new CertificateState(cert.NotAfter.ToUniversalTime(), renewAfter));
        }
        catch (Exception ex)
        {
            return Err<CertificateState?>(Error.FromException(ex, "agent.cert.load_failed"));
        }
    }

    private async ValueTask<Result<CertificateState>> RenewAsync(CancellationToken cancellationToken)
    {
        var csr = BuildCsr();
        if (csr.IsFailure)
        {
            return csr.CastFailure<CertificateState>();
        }

        (byte[] csrBytes, RSA key) = csr.Value;
        try
        {
            var request = new CsrRequest
            {
                NodeId = _options.NodeId,
                Csr = ByteString.CopyFrom(csrBytes)
            };

            var response = await _caClient.SubmitCsrAsync(request, cancellationToken).ConfigureAwait(false);
            var persisted = PersistCertificate(response, key);
            if (persisted.IsFailure)
            {
                return persisted;
            }

            return persisted;
        }
        catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested)
        {
            return Err<CertificateState>(Error.Canceled("Certificate renewal canceled", oce.CancellationToken));
        }
        catch (Exception ex)
        {
            return Err<CertificateState>(Error.FromException(ex, "agent.cert.renew_failed"));
        }
        finally
        {
            key.Dispose();
        }
    }

    private Result<(byte[] Csr, RSA Key)> BuildCsr()
    {
        try
        {
            var nodeId = _options.NodeId ?? Environment.MachineName;
            var subject = $"CN={nodeId}";
            var key = RSA.Create(_certOptions.KeySize);
            var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(nodeId);
            foreach (var dns in _certOptions.SanDns)
            {
                if (!string.IsNullOrWhiteSpace(dns))
                {
                    sanBuilder.AddDnsName(dns);
                }
            }

            foreach (var uriText in _certOptions.SanUris)
            {
                if (!string.IsNullOrWhiteSpace(uriText) && Uri.TryCreate(uriText, UriKind.Absolute, out var uri))
                {
                    sanBuilder.AddUri(uri);
                }
            }

            request.CertificateExtensions.Add(sanBuilder.Build());
            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: false));

            var csr = request.CreateSigningRequest();
            return Ok((csr, key));
        }
        catch (Exception ex)
        {
            return Err<(byte[] Csr, RSA Key)>(Error.FromException(ex, "agent.cert.csr_failed"));
        }
    }

    private Result<CertificateState> PersistCertificate(CertResponse response, RSA key)
    {
        try
        {
            using var leaf = X509CertificateLoader.LoadCertificate(response.Certificate.ToByteArray());
            using var withKey = leaf.CopyWithPrivateKey(key);
            var collection = new X509Certificate2Collection(withKey);
            if (response.CertificateChain.Length > 0)
            {
                collection.Add(X509CertificateLoader.LoadCertificate(response.CertificateChain.ToByteArray()));
            }

            var pfxBytes = collection.Export(X509ContentType.Pfx, _certOptions.PfxPassword);
            WriteAtomic(_certOptions.PfxPath, pfxBytes);

            if (!string.IsNullOrWhiteSpace(_certOptions.TrustBundlePath) && response.TrustBundle.Length > 0)
            {
                WriteAtomic(_certOptions.TrustBundlePath, response.TrustBundle.ToByteArray());
            }

            var expiresAt = ParseTimestamp(response.ExpiresAt) ?? withKey.NotAfter.ToUniversalTime();
            var renewAfter = ParseTimestamp(response.RenewAfter) ?? CalculateRenewAfter(withKey.NotBefore, withKey.NotAfter);

            return Ok(new CertificateState(expiresAt, renewAfter));
        }
        catch (Exception ex)
        {
            return Err<CertificateState>(Error.FromException(ex, "agent.cert.persist_failed"));
        }
    }

    private DateTimeOffset? ParseTimestamp(string? timestamp) =>
        DateTimeOffset.TryParse(timestamp, out var parsed) ? parsed.ToUniversalTime() : null;

    private DateTimeOffset CalculateRenewAfter(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var lifetime = notAfter - notBefore;
        if (lifetime <= TimeSpan.Zero)
        {
            lifetime = TimeSpan.FromHours(1);
        }

        var renewAfter = notBefore.ToUniversalTime() + TimeSpan.FromTicks((long)(lifetime.Ticks * _certOptions.RenewalWindow));
        return renewAfter > notAfter ? notAfter.ToUniversalTime() : renewAfter;
    }

    private static void WriteAtomic(string path, ReadOnlySpan<byte> data)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        using (var stream = new FileStream(
                   tempPath,
                   FileMode.Create,
                   FileAccess.Write,
                   FileShare.None,
                   16_384,
                   FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            stream.Write(data);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    private TimeSpan IncreaseBackoff(TimeSpan current)
    {
        var next = TimeSpan.FromMilliseconds(current.TotalMilliseconds * 2);
        var max = TimeSpan.FromMinutes(5);
        if (next > max)
        {
            return max;
        }

        return next;
    }

    public sealed record CertificateRenewalPlan(TimeSpan NextCheck, bool Renewed);

    public sealed record CertificateState(DateTimeOffset ExpiresAt, DateTimeOffset RenewAfter);
}
