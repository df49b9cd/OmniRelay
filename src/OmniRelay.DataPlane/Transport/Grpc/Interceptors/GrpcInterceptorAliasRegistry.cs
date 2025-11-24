using System.Collections.Concurrent;

namespace OmniRelay.Transport.Grpc.Interceptors;

/// <summary>
/// Trim/AOT-safe registry that maps interceptor aliases to concrete interceptor types.
/// </summary>
public interface IGrpcInterceptorAliasRegistry
{
    bool TryResolveServer(string aliasName, out Type type);
    void RegisterServer(string aliasName, Type interceptorType);
}

public sealed class GrpcInterceptorAliasRegistry : IGrpcInterceptorAliasRegistry
{
    private readonly ConcurrentDictionary<string, Type> _server = new(StringComparer.OrdinalIgnoreCase);

    public GrpcInterceptorAliasRegistry()
    {
        // populated by mapper with built-ins; tests/hosts can add more via DI.
    }

    public bool TryResolveServer(string aliasName, out Type type) => _server.TryGetValue(aliasName, out type!);

    public void RegisterServer(string aliasName, Type interceptorType)
    {
        ArgumentException.ThrowIfNullOrEmpty(aliasName);
        ArgumentNullException.ThrowIfNull(interceptorType);
        _server[aliasName] = interceptorType;
    }
}
