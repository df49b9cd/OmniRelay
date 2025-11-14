namespace OmniRelay.Security.Secrets;

/// <summary>Configuration for <see cref="FileSecretProvider"/>.</summary>
public sealed class FileSecretProviderOptions
{
    /// <summary>Base directory used for relative paths. Defaults to <see cref="AppContext.BaseDirectory"/>.</summary>
    public string BaseDirectory { get; set; } = AppContext.BaseDirectory;

    /// <summary>Named secrets mapped to file paths.</summary>
    public IDictionary<string, string> Secrets { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
