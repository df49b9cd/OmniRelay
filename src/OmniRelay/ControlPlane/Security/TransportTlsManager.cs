using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using OmniRelay.Security.Secrets;

namespace OmniRelay.ControlPlane.Security;

/// <summary>
/// Loads and refreshes TLS certificates for control-plane transports (gRPC/HTTP/gossip).
/// Provides a single source of truth for both server and client credentials.
/// </summary>
public sealed class TransportTlsManager : IDisposable
{
    private readonly TransportTlsOptions _options;
    private readonly ILogger<TransportTlsManager> _logger;
    private readonly ISecretProvider? _secretProvider;
    private readonly object _lock = new();
    private X509Certificate2? _certificate;
    private DateTimeOffset _lastLoaded;
    private DateTime _lastWrite;
    private IDisposable? _dataReloadRegistration;
    private IDisposable? _passwordReloadRegistration;
    private static readonly Action<ILogger, string, string, Exception?> CertificateLoadedLog =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, "TransportCertificateLoaded"),
            "Control-plane TLS certificate loaded from {Source}. Subject={Subject}");

    public TransportTlsManager(
        TransportTlsOptions options,
        ILogger<TransportTlsManager> logger,
        ISecretProvider? secretProvider = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretProvider = secretProvider;
    }

    /// <summary>Returns true when a certificate source was configured.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.CertificatePath) ||
        !string.IsNullOrWhiteSpace(_options.CertificateData) ||
        !string.IsNullOrWhiteSpace(_options.CertificateDataSecret);

    /// <summary>
    /// Retrieves the latest certificate instance, reloading from disk/inline data when necessary.
    /// The caller takes ownership over the returned <see cref="X509Certificate2"/>.
    /// </summary>
    public X509Certificate2 GetCertificate()
    {
        lock (_lock)
        {
            if (_certificate is null || ShouldReloadLocked())
            {
                ReloadLocked();
            }

            return new X509Certificate2(_certificate!);
        }
    }

    private bool ShouldReloadLocked()
    {
        if (!IsConfigured)
        {
            return false;
        }

        if (_certificate is null)
        {
            return true;
        }

        // Inline certificates (including secret-backed) reload when their change tokens fire.
        if (!string.IsNullOrWhiteSpace(_options.CertificateData) ||
            !string.IsNullOrWhiteSpace(_options.CertificateDataSecret))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (_options.ReloadInterval is { } interval &&
            interval > TimeSpan.Zero &&
            now - _lastLoaded >= interval)
        {
            return true;
        }

        var path = ResolveCertificatePath();
        if (!File.Exists(path))
        {
            return false;
        }

        var write = File.GetLastWriteTimeUtc(path);
        return write > _lastWrite;
    }

    private void ReloadLocked()
    {
        var flags = _options.KeyStorageFlags;
        X509Certificate2 certificate;
        string source;
        DateTime? lastWrite = null;

        var inlineBytes = TryLoadInlineCertificate(out source);
        var password = ResolveCertificatePassword();

        if (inlineBytes is not null)
        {
            try
            {
                certificate = X509CertificateLoader.LoadPkcs12(inlineBytes, password, flags);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(inlineBytes);
                TransportTlsManagerTestHooks.NotifySecretsCleared(inlineBytes);
            }
        }
        else
        {
            var path = ResolveCertificatePath();
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Transport TLS certificate '{path}' was not found.");
            }

            var raw = File.ReadAllBytes(path);
            certificate = X509CertificateLoader.LoadPkcs12(raw, password, flags);
            source = path;
            lastWrite = File.GetLastWriteTimeUtc(path);
        }

        _certificate?.Dispose();
        _certificate = certificate;
        _lastLoaded = DateTimeOffset.UtcNow;
        _lastWrite = lastWrite ?? DateTime.MinValue;
        CertificateLoadedLog(_logger, source, certificate.Subject, null);
    }

    private byte[]? TryLoadInlineCertificate(out string source)
    {
        if (!string.IsNullOrWhiteSpace(_options.CertificateData))
        {
            source = "inline certificate data";
            return DecodeBase64(_options.CertificateData);
        }

        if (string.IsNullOrWhiteSpace(_options.CertificateDataSecret))
        {
            source = string.Empty;
            return null;
        }

        using var secret = AcquireSecret(_options.CertificateDataSecret, "transport TLS certificate data");
        RegisterSecretReload(ref _dataReloadRegistration, secret.ChangeToken, $"secret:{_options.CertificateDataSecret}");
        source = $"secret:{_options.CertificateDataSecret}";
        return DecodeSecretBytes(secret);
    }

    private string? ResolveCertificatePassword()
    {
        if (!string.IsNullOrWhiteSpace(_options.CertificatePassword))
        {
            return _options.CertificatePassword;
        }

        if (string.IsNullOrWhiteSpace(_options.CertificatePasswordSecret))
        {
            return null;
        }

        using var secret = AcquireSecret(_options.CertificatePasswordSecret, "transport TLS certificate password");
        RegisterSecretReload(ref _passwordReloadRegistration, secret.ChangeToken, $"secret:{_options.CertificatePasswordSecret}");
        var password = secret.AsString();
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException($"Secret '{_options.CertificatePasswordSecret}' did not contain a TLS password.");
        }

        return password;
    }

    private SecretValue AcquireSecret(string name, string purpose)
    {
        if (_secretProvider is null)
        {
            throw new InvalidOperationException($"{purpose} references secret '{name}' but no secret provider is configured.");
        }

        var secret = _secretProvider.GetSecretAsync(name).GetAwaiter().GetResult();
        if (secret is null)
        {
            throw new InvalidOperationException($"Secret '{name}' required for {purpose} was not found.");
        }

        return secret;
    }

    private static byte[] DecodeBase64(string data)
    {
        try
        {
            return Convert.FromBase64String(data);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("transport TLS certificate data is not valid Base64.", ex);
        }
    }

    private static byte[] DecodeSecretBytes(SecretValue secret)
    {
        var text = secret.AsString();
        if (!string.IsNullOrWhiteSpace(text))
        {
            return DecodeBase64(text);
        }

        var memory = secret.AsMemory();
        if (memory.IsEmpty)
        {
            throw new InvalidOperationException("transport TLS certificate secret had no payload.");
        }

        return memory.ToArray();
    }

    private void RegisterSecretReload(ref IDisposable? registration, IChangeToken? token, string description)
    {
        registration?.Dispose();
        if (token is null)
        {
            registration = null;
            return;
        }

        registration = token.RegisterChangeCallback(static state =>
        {
            var (manager, reason) = ((TransportTlsManager, string))state!;
            manager.HandleSecretRotation(reason);
        }, (this, description));
    }

    private void HandleSecretRotation(string description)
    {
        lock (_lock)
        {
            _certificate?.Dispose();
            _certificate = null;
            _lastLoaded = DateTimeOffset.MinValue;
            _lastWrite = DateTime.MinValue;
            _logger.LogInformation("Control-plane TLS secret {SecretDescription} changed. Certificate will reload on next access.", description);
        }
    }

    private string ResolveCertificatePath()
    {
        var path = _options.CertificatePath ?? throw new InvalidOperationException("A transport TLS certificate path must be configured.");
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(AppContext.BaseDirectory, path);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _certificate?.Dispose();
            _certificate = null;
            _dataReloadRegistration?.Dispose();
            _dataReloadRegistration = null;
            _passwordReloadRegistration?.Dispose();
            _passwordReloadRegistration = null;
        }
    }
}

internal static class TransportTlsManagerTestHooks
{
    public static Action<byte[]>? SecretsCleared { get; set; }

    public static void NotifySecretsCleared(byte[] buffer)
    {
        SecretsCleared?.Invoke(buffer);
    }
}
