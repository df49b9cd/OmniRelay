using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace OmniRelay.Security.Secrets;

/// <summary>Loads secrets from encrypted files on disk.</summary>
public sealed class FileSecretProvider : ISecretProvider, IDisposable
{
    private readonly FileSecretProviderOptions _options;
    private readonly ISecretAccessAuditor _auditor;
    private readonly ConcurrentDictionary<string, PhysicalFileProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public FileSecretProvider(FileSecretProviderOptions options, ISecretAccessAuditor auditor)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _auditor = auditor ?? throw new ArgumentNullException(nameof(auditor));
    }

    public ValueTask<SecretValue?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_options.Secrets.TryGetValue(name, out var path))
        {
            _auditor.RecordAccess("file", name, SecretAccessOutcome.NotFound);
            return ValueTask.FromResult<SecretValue?>(null);
        }

        var resolved = ResolvePath(path);
        if (!File.Exists(resolved))
        {
            _auditor.RecordAccess("file", name, SecretAccessOutcome.NotFound);
            return ValueTask.FromResult<SecretValue?>(null);
        }

        var buffer = File.ReadAllBytes(resolved);
        var metadata = new SecretMetadata(
            name,
            "file",
            DateTimeOffset.UtcNow,
            FromCache: false,
            Version: File.GetLastWriteTimeUtc(resolved).ToString("O"));

        var secret = new SecretValue(metadata, buffer, Watch(name));
        _auditor.RecordAccess("file", name, SecretAccessOutcome.Success);
        return ValueTask.FromResult<SecretValue?>(secret);
    }

    public IChangeToken? Watch(string name)
    {
        if (!_options.Secrets.TryGetValue(name, out var path))
        {
            return null;
        }

        var resolved = ResolvePath(path);
        var directory = Path.GetDirectoryName(resolved);
        var fileName = Path.GetFileName(resolved);

        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var provider = _providers.GetOrAdd(directory ?? string.Empty, key =>
        {
            var baseDir = string.IsNullOrEmpty(key) ? _options.BaseDirectory : key;
            return new PhysicalFileProvider(baseDir);
        });

        return provider.Watch(string.IsNullOrEmpty(directory) ? fileName : fileName);
    }

    private string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Secret path cannot be null or whitespace.", nameof(path));
        }

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(_options.BaseDirectory, path);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var provider in _providers.Values)
        {
            provider.Dispose();
        }

        _providers.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
