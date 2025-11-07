using System;
using System.IO;

namespace OmniRelay.IntegrationTests.Support;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "omnirelay-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Resolve(params string[] segments)
    {
        if (segments is null || segments.Length == 0)
        {
            return Path;
        }

        var combined = Path;
        foreach (var segment in segments)
        {
            combined = System.IO.Path.Combine(combined, segment);
        }

        return combined;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
        }
    }
}
