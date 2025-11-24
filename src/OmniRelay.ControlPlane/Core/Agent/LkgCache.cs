using System.Text.Json;
using System.Text.Json.Serialization;
using Hugo;
using Unit = Hugo.Go.Unit;

namespace OmniRelay.ControlPlane.Agent;

/// <summary>Persists last-known-good control snapshot to disk for agent/edge resilience.</summary>
public sealed class LkgCache
{
    private readonly string _path;

    internal sealed record LkgEnvelope(string Version, long Epoch, byte[] Payload, byte[] ResumeToken);

    public LkgCache(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public ValueTask<Result<Unit>> SaveAsync(string version, long epoch, ReadOnlyMemory<byte> payload, ReadOnlyMemory<byte> resumeToken, CancellationToken cancellationToken = default)
    {
        return Result.TryAsync<Unit>(async ct =>
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var envelope = new LkgEnvelope(version, epoch, payload.ToArray(), resumeToken.ToArray());

            var stream = new FileStream(
                _path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                16_384,
                FileOptions.Asynchronous | FileOptions.WriteThrough);

            try
            {
                await JsonSerializer.SerializeAsync(stream, envelope, LkgCacheJsonContext.Default.LkgEnvelope, ct).ConfigureAwait(false);
                await stream.FlushAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }

            return Unit.Value;
        }, cancellationToken: cancellationToken);
    }

    public ValueTask<Result<LkgSnapshot?>> TryLoadAsync(CancellationToken cancellationToken = default)
    {
        return Result.TryAsync<LkgSnapshot?>(async ct =>
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            var stream = new FileStream(
                _path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                16_384,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            try
            {
                var envelope = await JsonSerializer.DeserializeAsync(stream, LkgCacheJsonContext.Default.LkgEnvelope, ct).ConfigureAwait(false);
                if (envelope is null)
                {
                    return null;
                }

                return new LkgSnapshot(envelope.Version, envelope.Epoch, envelope.Payload, envelope.ResumeToken);
            }
            finally
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }, cancellationToken: cancellationToken);
    }
}

public sealed record LkgSnapshot(string Version, long Epoch, byte[] Payload, byte[] ResumeToken);

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(LkgCache.LkgEnvelope))]
internal partial class LkgCacheJsonContext : JsonSerializerContext
{
}
