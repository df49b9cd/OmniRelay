using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Hugo;
using OmniRelay.Core;
using static Hugo.Go;

namespace OmniRelay.Dispatcher;

/// <summary>
/// Thread-safe registry that resolves codecs for inbound and outbound procedures.
/// </summary>
public sealed class CodecRegistry
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly Dictionary<ProcedureCodecKey, ProcedureCodecDescriptor> _codecs = new();
    private readonly Lock _gate = new();
    private readonly string _localService;

    private CodecRegistry(string localService)
    {
        _localService = localService;
    }

    internal static Result<CodecRegistry> Create(string localService, IEnumerable<ProcedureCodecRegistration> registrations)
    {
        if (string.IsNullOrWhiteSpace(localService))
        {
            return Err<CodecRegistry>(CodecRegistryErrors.LocalServiceRequired());
        }

        var registry = new CodecRegistry(localService);
        foreach (var registration in registrations)
        {
            var service = registry.ResolveService(registration.Scope, registration.Service);
            var descriptor = CreateDescriptor(registration);
            var addResult = registry.RegisterInternal(
                registration.Scope,
                service,
                registration.Procedure,
                registration.Kind,
                descriptor,
                registration.Aliases);

            if (addResult.IsFailure)
            {
                return Err<CodecRegistry>(DispatcherErrors.CodecRegistrationFailed(addResult.Error!));
            }
        }

        return Ok(registry);
    }

    internal static Result<CodecRegistry> Create(string localService)
        => Create(localService, Array.Empty<ProcedureCodecRegistration>());

    /// <summary>
    /// Registers a codec for an inbound procedure on the local service.
    /// </summary>
    public Result<Unit> RegisterInbound<TRequest, TResponse>(
        string procedure,
        ProcedureKind kind,
        ICodec<TRequest, TResponse> codec,
        IEnumerable<string>? aliases = null) => Register(
            ProcedureCodecScope.Inbound,
            _localService,
            procedure,
            kind,
            codec,
            aliases);

    /// <summary>
    /// Registers a codec for an outbound procedure on the specified remote service.
    /// </summary>
    public Result<Unit> RegisterOutbound<TRequest, TResponse>(
        string service,
        string procedure,
        ProcedureKind kind,
        ICodec<TRequest, TResponse> codec,
        IEnumerable<string>? aliases = null)
    {
        if (string.IsNullOrWhiteSpace(service))
        {
            return Err<Unit>(CodecRegistryErrors.ServiceRequired());
        }

        return Register(
            ProcedureCodecScope.Outbound,
            service,
            procedure,
            kind,
            codec,
            aliases);
    }

    /// <summary>
    /// Attempts to resolve a codec descriptor for the specified procedure.
    /// </summary>
    public bool TryResolve(
        ProcedureCodecScope scope,
        string service,
        string procedure,
        ProcedureKind kind,
        [MaybeNullWhen(false)] out ProcedureCodecDescriptor descriptor)
    {
        descriptor = default!;

        if (string.IsNullOrWhiteSpace(service))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(procedure))
        {
            return false;
        }

        var key = new ProcedureCodecKey(scope, service, procedure, kind);

        lock (_gate)
        {
            return _codecs.TryGetValue(key, out descriptor);
        }
    }

    /// <summary>
    /// Attempts to resolve a strongly typed codec for the specified procedure.
    /// </summary>
    public bool TryResolve<TRequest, TResponse>(
        ProcedureCodecScope scope,
        string service,
        string procedure,
        ProcedureKind kind,
        out ICodec<TRequest, TResponse> codec)
    {
        codec = default!;

        if (!TryResolve(scope, service, procedure, kind, out var descriptor))
        {
            return false;
        }

        EnsureTypeMatch(descriptor, typeof(TRequest), typeof(TResponse), service, procedure, kind, scope);

        codec = (ICodec<TRequest, TResponse>)descriptor.Codec;
        return true;
    }

    /// <summary>
    /// Returns an immutable snapshot of all registered codecs.
    /// </summary>
    public ImmutableArray<(ProcedureCodecScope Scope, string Service, string Procedure, ProcedureKind Kind, ProcedureCodecDescriptor Descriptor)> Snapshot()
    {
        lock (_gate)
        {
            var builder = ImmutableArray.CreateBuilder<(ProcedureCodecScope, string, string, ProcedureKind, ProcedureCodecDescriptor)>(_codecs.Count);
            foreach (var entry in _codecs)
            {
                builder.Add((entry.Key.Scope, entry.Key.Service, entry.Key.Procedure, entry.Key.Kind, entry.Value));
            }

            return builder.ToImmutable();
        }
    }

    private Result<Unit> Register<TRequest, TResponse>(
        ProcedureCodecScope scope,
        string service,
        string procedure,
        ProcedureKind kind,
        ICodec<TRequest, TResponse> codec,
        IEnumerable<string>? aliases)
    {
        ArgumentNullException.ThrowIfNull(codec);

        return RegisterInternal(
            scope,
            service,
            procedure,
            kind,
            new ProcedureCodecDescriptor(typeof(TRequest), typeof(TResponse), codec, codec.Encoding),
            aliases);
    }

    private Result<Unit> RegisterInternal(
        ProcedureCodecScope scope,
        string service,
        string procedure,
        ProcedureKind kind,
        ProcedureCodecDescriptor descriptor,
        ImmutableArray<string> aliases) => RegisterInternal(scope, service, procedure, kind, descriptor, (IEnumerable<string>)aliases);

    private Result<Unit> RegisterInternal(
        ProcedureCodecScope scope,
        string service,
        string procedure,
        ProcedureKind kind,
        ProcedureCodecDescriptor descriptor,
        IEnumerable<string>? aliases = null)
    {
        if (string.IsNullOrWhiteSpace(service))
        {
            return Err<Unit>(CodecRegistryErrors.ServiceRequired());
        }

        if (string.IsNullOrWhiteSpace(procedure))
        {
            return Err<Unit>(CodecRegistryErrors.ProcedureRequired());
        }

        lock (_gate)
        {
            foreach (var name in EnumerateNames(procedure, aliases))
            {
                var key = new ProcedureCodecKey(scope, service, name, kind);
                if (_codecs.ContainsKey(key))
                {
                    return Err<Unit>(CodecRegistryErrors.Duplicate(scope.ToString(), service, name, kind));
                }

                _codecs[key] = descriptor;
            }
        }

        return Ok(Unit.Value);
    }

    private static ProcedureCodecDescriptor CreateDescriptor(ProcedureCodecRegistration registration) =>
        new(registration.RequestType, registration.ResponseType, registration.Codec, registration.Encoding);

    private string ResolveService(ProcedureCodecScope scope, string? service) =>
        scope switch
        {
            ProcedureCodecScope.Inbound => _localService,
            _ => service ?? string.Empty
        };

    private static IEnumerable<string> EnumerateNames(string procedure, IEnumerable<string>? aliases)
    {
        yield return procedure;

        if (aliases is null)
        {
            yield break;
        }

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                continue;
            }

            yield return alias;
        }
    }

    private static void EnsureTypeMatch(
        ProcedureCodecDescriptor descriptor,
        Type requestType,
        Type responseType,
        string service,
        string procedure,
        ProcedureKind kind,
        ProcedureCodecScope scope)
    {
        if (descriptor.RequestType != requestType || descriptor.ResponseType != responseType)
        {
            throw new InvalidOperationException(
                $"Codec registered for {scope} procedure '{service}::{procedure}' ({kind}) expects request '{descriptor.RequestType.FullName}' and response '{descriptor.ResponseType.FullName}', but caller requested '{requestType.FullName}' → '{responseType.FullName}'.");
        }
    }

    private readonly struct ProcedureCodecKey : IEquatable<ProcedureCodecKey>
    {
        public ProcedureCodecKey(ProcedureCodecScope scope, string service, string procedure, ProcedureKind kind)
        {
            Scope = scope;
            Service = service?.Trim() ?? string.Empty;
            Procedure = procedure?.Trim() ?? string.Empty;
            Kind = kind;
        }

        public ProcedureCodecScope Scope { get; }
        public string Service { get; }
        public string Procedure { get; }
        public ProcedureKind Kind { get; }

        public bool Equals(ProcedureCodecKey other) =>
            Scope == other.Scope &&
            Comparer.Equals(Service, other.Service) &&
            Comparer.Equals(Procedure, other.Procedure) &&
            Kind == other.Kind;

        public override bool Equals(object? obj) => obj is ProcedureCodecKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add((int)Scope);
            hash.Add(Service, Comparer);
            hash.Add(Procedure, Comparer);
            hash.Add((int)Kind);
            return hash.ToHashCode();
        }
    }
}
