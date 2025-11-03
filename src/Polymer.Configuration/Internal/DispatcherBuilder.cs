using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polymer.Configuration.Models;
using Polymer.Core.Middleware;
using Polymer.Core.Peers;
using Polymer.Dispatcher;
using Polymer.Transport.Grpc;
using Polymer.Transport.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Polymer.Configuration.Internal;

internal sealed class DispatcherBuilder
{
    private readonly PolymerConfigurationOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, HttpOutbound> _httpOutboundCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GrpcOutbound> _grpcOutboundCache = new(StringComparer.OrdinalIgnoreCase);

    public DispatcherBuilder(PolymerConfigurationOptions options, IServiceProvider serviceProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Dispatcher.Dispatcher Build()
    {
        var serviceName = _options.Service?.Trim();
        if (string.IsNullOrEmpty(serviceName))
        {
            throw new PolymerConfigurationException("Polymer configuration must specify a service name (service).");
        }

        var dispatcherOptions = new DispatcherOptions(serviceName);

        ApplyMiddleware(dispatcherOptions);
        ConfigureInbounds(dispatcherOptions);
        ConfigureOutbounds(dispatcherOptions);

        return new Dispatcher.Dispatcher(dispatcherOptions);
    }

    private void ConfigureInbounds(DispatcherOptions dispatcherOptions)
    {
        ConfigureHttpInbounds(dispatcherOptions);
        ConfigureGrpcInbounds(dispatcherOptions);
    }

    private void ConfigureHttpInbounds(DispatcherOptions dispatcherOptions)
    {
        if (_options.Inbounds is null)
        {
            return;
        }

        var index = 0;
        foreach (var inbound in _options.Inbounds.Http)
        {
            if (inbound is null)
            {
                index++;
                continue;
            }

            if (inbound.Urls.Count == 0)
            {
                throw new PolymerConfigurationException($"HTTP inbound at index {index} must specify at least one url.");
            }

            var urls = inbound.Urls
                .Select((url, position) => ValidateHttpUrl(url, $"http inbound #{index} (entry {position})"))
                .ToArray();

            if (urls.Length == 0)
            {
                throw new PolymerConfigurationException($"HTTP inbound at index {index} resolved to zero valid urls.");
            }

            var name = string.IsNullOrWhiteSpace(inbound.Name)
                ? $"http-inbound:{index}"
                : inbound.Name!;

            dispatcherOptions.AddLifecycle(name, new HttpInbound(urls));
            index++;
        }
    }

    private void ConfigureGrpcInbounds(DispatcherOptions dispatcherOptions)
    {
        if (_options.Inbounds is null)
        {
            return;
        }

        var index = 0;
        foreach (var inbound in _options.Inbounds.Grpc)
        {
            if (inbound is null)
            {
                index++;
                continue;
            }

            if (inbound.Urls.Count == 0)
            {
                throw new PolymerConfigurationException($"gRPC inbound at index {index} must specify at least one url.");
            }

            var urls = inbound.Urls
                .Select((url, position) => ValidateGrpcUrl(url, $"grpc inbound #{index} (entry {position})"))
                .ToArray();

            var runtimeOptions = BuildGrpcServerRuntimeOptions(inbound.Runtime);
            var tlsOptions = BuildGrpcServerTlsOptions(inbound.Tls);
            var telemetryOptions = BuildGrpcTelemetryOptions(inbound.Telemetry, serverSide: true);

            var name = string.IsNullOrWhiteSpace(inbound.Name)
                ? $"grpc-inbound:{index}"
                : inbound.Name!;

            dispatcherOptions.AddLifecycle(
                name,
                new GrpcInbound(
                    urls,
                    serverRuntimeOptions: runtimeOptions,
                    serverTlsOptions: tlsOptions,
                    telemetryOptions: telemetryOptions));

            index++;
        }
    }

    private void ConfigureOutbounds(DispatcherOptions dispatcherOptions)
    {
        foreach (var (service, config) in _options.Outbounds)
        {
            if (string.IsNullOrWhiteSpace(service) || config is null)
            {
                continue;
            }

            RegisterOutboundSet(dispatcherOptions, service, config.Unary, OutboundKind.Unary);
            RegisterOutboundSet(dispatcherOptions, service, config.Oneway, OutboundKind.Oneway);
            RegisterOutboundSet(dispatcherOptions, service, config.Stream, OutboundKind.Stream);
            RegisterOutboundSet(dispatcherOptions, service, config.ClientStream, OutboundKind.ClientStream);
            RegisterOutboundSet(dispatcherOptions, service, config.Duplex, OutboundKind.Duplex);
        }
    }

    private void RegisterOutboundSet(
        DispatcherOptions dispatcherOptions,
        string service,
        RpcOutboundConfiguration? configuration,
        OutboundKind kind)
    {
        if (configuration is null)
        {
            return;
        }

        foreach (var http in configuration.Http)
        {
            if (http is null)
            {
                continue;
            }

            if (kind is OutboundKind.Stream or OutboundKind.ClientStream or OutboundKind.Duplex)
            {
                throw new PolymerConfigurationException(
                    $"HTTP outbound cannot satisfy {kind.ToString().ToLowerInvariant()} RPCs for service '{service}'.");
            }

            var outbound = CreateHttpOutbound(service, http);
            switch (kind)
            {
                case OutboundKind.Unary:
                    dispatcherOptions.AddUnaryOutbound(service, http.Key, outbound);
                    break;
                case OutboundKind.Oneway:
                    dispatcherOptions.AddOnewayOutbound(service, http.Key, outbound);
                    break;
            }
        }

        foreach (var grpc in configuration.Grpc)
        {
            if (grpc is null)
            {
                continue;
            }

            var outbound = CreateGrpcOutbound(service, grpc);
            switch (kind)
            {
                case OutboundKind.Unary:
                    dispatcherOptions.AddUnaryOutbound(service, grpc.Key, outbound);
                    break;
                case OutboundKind.Oneway:
                    dispatcherOptions.AddOnewayOutbound(service, grpc.Key, outbound);
                    break;
                case OutboundKind.Stream:
                    dispatcherOptions.AddStreamOutbound(service, grpc.Key, outbound);
                    break;
                case OutboundKind.ClientStream:
                    dispatcherOptions.AddClientStreamOutbound(service, grpc.Key, outbound);
                    break;
                case OutboundKind.Duplex:
                    dispatcherOptions.AddDuplexOutbound(service, grpc.Key, outbound);
                    break;
            }
        }
    }

    private HttpOutbound CreateHttpOutbound(string service, HttpOutboundTargetConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.Url))
        {
            throw new PolymerConfigurationException($"HTTP outbound for service '{service}' must specify a url.");
        }

        var uri = ValidateHttpUrl(configuration.Url!, $"http outbound for service '{service}'");
        var cacheKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{service}|{configuration.Key ?? OutboundCollection.DefaultKey}|{uri}|{configuration.ClientName ?? string.Empty}");

        if (_httpOutboundCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var (client, dispose) = CreateHttpClient(configuration.ClientName);
        var outbound = new HttpOutbound(client, new Uri(uri, UriKind.Absolute), dispose);
        _httpOutboundCache[cacheKey] = outbound;
        return outbound;
    }

    private (HttpClient Client, bool Dispose) CreateHttpClient(string? clientName)
    {
        var factory = _serviceProvider.GetService<IHttpClientFactory>();
        if (factory is not null)
        {
            var name = string.IsNullOrWhiteSpace(clientName) ? string.Empty : clientName!;
            var client = string.IsNullOrEmpty(name)
                ? factory.CreateClient()
                : factory.CreateClient(name);
            return (client, false);
        }

        return (new HttpClient(), true);
    }

    private GrpcOutbound CreateGrpcOutbound(string service, GrpcOutboundTargetConfiguration configuration)
    {
        if (configuration.Addresses.Count == 0)
        {
            throw new PolymerConfigurationException($"gRPC outbound for service '{service}' must specify peer addresses.");
        }

        var uris = configuration.Addresses
            .Select((address, index) => ValidateGrpcUrl(address, $"grpc outbound peer #{index} for service '{service}'"))
            .Select(value => new Uri(value, UriKind.Absolute))
            .ToArray();

        var remoteService = string.IsNullOrWhiteSpace(configuration.RemoteService)
            ? service
            : configuration.RemoteService!;

        var cacheKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{service}|{configuration.Key ?? OutboundCollection.DefaultKey}|{remoteService}|{string.Join(",", uris.Select(u => u.ToString()))}|{configuration.PeerChooser ?? "round-robin"}");

        if (_grpcOutboundCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var chooserFactory = CreatePeerChooserFactory(configuration.PeerChooser);
        var breakerOptions = BuildCircuitBreakerOptions(configuration.CircuitBreaker);
        var telemetryOptions = BuildGrpcTelemetryOptions(configuration.Telemetry, serverSide: false);
        var runtimeOptions = BuildGrpcClientRuntimeOptions(configuration.Runtime);
        var tlsOptions = BuildGrpcClientTlsOptions(configuration.Tls);

        var outbound = new GrpcOutbound(
            uris,
            remoteService,
            clientTlsOptions: tlsOptions,
            peerChooser: chooserFactory,
            clientRuntimeOptions: runtimeOptions,
            peerCircuitBreakerOptions: breakerOptions,
            telemetryOptions: telemetryOptions);

        _grpcOutboundCache[cacheKey] = outbound;
        return outbound;
    }

    private static Func<IReadOnlyList<IPeer>, IPeerChooser> CreatePeerChooserFactory(string? peerChooser)
    {
        if (string.IsNullOrWhiteSpace(peerChooser) ||
            peerChooser.Equals("round-robin", StringComparison.OrdinalIgnoreCase) ||
            peerChooser.Equals("roundrobin", StringComparison.OrdinalIgnoreCase))
        {
            return peers => new RoundRobinPeerChooser(ImmutableArray.CreateRange(peers));
        }

        if (peerChooser.Equals("fewest-pending", StringComparison.OrdinalIgnoreCase) ||
            peerChooser.Equals("least-pending", StringComparison.OrdinalIgnoreCase))
        {
            return peers => new FewestPendingPeerChooser(ImmutableArray.CreateRange(peers));
        }

        if (peerChooser.Equals("two-random-choice", StringComparison.OrdinalIgnoreCase) ||
            peerChooser.Equals("two-random-choices", StringComparison.OrdinalIgnoreCase) ||
            peerChooser.Equals("2-random", StringComparison.OrdinalIgnoreCase))
        {
            return peers => new TwoRandomPeerChooser(ImmutableArray.CreateRange(peers));
        }

        throw new PolymerConfigurationException(
            $"Unsupported peer chooser '{peerChooser}'. Supported values: round-robin, fewest-pending, two-random-choice.");
    }

    private static PeerCircuitBreakerOptions? BuildCircuitBreakerOptions(PeerCircuitBreakerConfiguration configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        var hasValues =
            configuration.BaseDelay.HasValue ||
            configuration.MaxDelay.HasValue ||
            configuration.FailureThreshold.HasValue ||
            configuration.HalfOpenMaxAttempts.HasValue ||
            configuration.HalfOpenSuccessThreshold.HasValue;

        if (!hasValues)
        {
            return null;
        }

        var defaults = new PeerCircuitBreakerOptions();

        return new PeerCircuitBreakerOptions
        {
            BaseDelay = configuration.BaseDelay ?? defaults.BaseDelay,
            MaxDelay = configuration.MaxDelay ?? defaults.MaxDelay,
            FailureThreshold = configuration.FailureThreshold ?? defaults.FailureThreshold,
            HalfOpenMaxAttempts = configuration.HalfOpenMaxAttempts ?? defaults.HalfOpenMaxAttempts,
            HalfOpenSuccessThreshold = configuration.HalfOpenSuccessThreshold ?? defaults.HalfOpenSuccessThreshold,
            TimeProvider = defaults.TimeProvider
        };
    }

    private GrpcClientTlsOptions? BuildGrpcClientTlsOptions(GrpcClientTlsConfiguration configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        var hasValues =
            !string.IsNullOrWhiteSpace(configuration.CertificatePath) ||
            !string.IsNullOrWhiteSpace(configuration.CertificatePassword) ||
            configuration.AllowUntrustedCertificates.HasValue ||
            !string.IsNullOrWhiteSpace(configuration.TargetNameOverride);

        if (!hasValues)
        {
            return null;
        }

        var certificates = new X509Certificate2Collection();
        if (!string.IsNullOrWhiteSpace(configuration.CertificatePath))
        {
            var path = ResolvePath(configuration.CertificatePath!);
            if (!File.Exists(path))
            {
                throw new PolymerConfigurationException($"Client TLS certificate file '{path}' could not be found.");
            }

            X509Certificate2 cert;
#pragma warning disable SYSLIB0057
            cert = string.IsNullOrEmpty(configuration.CertificatePassword)
                ? new X509Certificate2(path)
                : new X509Certificate2(path, configuration.CertificatePassword);
#pragma warning restore SYSLIB0057
            certificates.Add(cert);
        }

        if (!string.IsNullOrWhiteSpace(configuration.TargetNameOverride))
        {
            throw new PolymerConfigurationException("gRPC client target name override is not yet supported in configuration.");
        }

        RemoteCertificateValidationCallback? validationCallback = null;
        if (configuration.AllowUntrustedCertificates == true)
        {
            validationCallback = (_, _, _, _) => true;
        }

        return new GrpcClientTlsOptions
        {
            ClientCertificates = certificates,
            ServerCertificateValidationCallback = validationCallback
        };
    }

    private GrpcClientRuntimeOptions? BuildGrpcClientRuntimeOptions(GrpcClientRuntimeConfiguration configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        var interceptors = ResolveClientInterceptors(configuration.Interceptors);

        var hasValues =
            configuration.MaxReceiveMessageSize.HasValue ||
            configuration.MaxSendMessageSize.HasValue ||
            configuration.KeepAlivePingDelay.HasValue ||
            configuration.KeepAlivePingTimeout.HasValue ||
            interceptors.Count > 0;

        if (!hasValues)
        {
            return null;
        }

        return new GrpcClientRuntimeOptions
        {
            MaxReceiveMessageSize = configuration.MaxReceiveMessageSize,
            MaxSendMessageSize = configuration.MaxSendMessageSize,
            KeepAlivePingDelay = configuration.KeepAlivePingDelay,
            KeepAlivePingTimeout = configuration.KeepAlivePingTimeout,
            Interceptors = interceptors
        };
    }

    private IReadOnlyList<Interceptor> ResolveClientInterceptors(IEnumerable<string> typeNames)
    {
        var resolved = new List<Interceptor>();
        foreach (var typeName in typeNames)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                continue;
            }

            var type = ResolveType(typeName);
            if (!typeof(Interceptor).IsAssignableFrom(type))
            {
                throw new PolymerConfigurationException(
                    $"Configured gRPC client interceptor '{typeName}' does not derive from {nameof(Interceptor)}.");
            }

            var instance = (Interceptor)ActivatorUtilities.CreateInstance(_serviceProvider, type);
            resolved.Add(instance);
        }

        return resolved;
    }

    private GrpcServerRuntimeOptions? BuildGrpcServerRuntimeOptions(GrpcServerRuntimeConfiguration configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        var interceptors = ResolveServerInterceptorTypes(configuration.Interceptors);
        var hasValues =
            configuration.MaxReceiveMessageSize.HasValue ||
            configuration.MaxSendMessageSize.HasValue ||
            configuration.EnableDetailedErrors.HasValue ||
            configuration.KeepAlivePingDelay.HasValue ||
            configuration.KeepAlivePingTimeout.HasValue ||
            interceptors.Count > 0;

        if (!hasValues)
        {
            return null;
        }

        return new GrpcServerRuntimeOptions
        {
            MaxReceiveMessageSize = configuration.MaxReceiveMessageSize,
            MaxSendMessageSize = configuration.MaxSendMessageSize,
            KeepAlivePingDelay = configuration.KeepAlivePingDelay,
            KeepAlivePingTimeout = configuration.KeepAlivePingTimeout,
            EnableDetailedErrors = configuration.EnableDetailedErrors,
            Interceptors = interceptors
        };
    }

    private IReadOnlyList<Type> ResolveServerInterceptorTypes(IEnumerable<string> typeNames)
    {
        var resolved = new List<Type>();
        foreach (var typeName in typeNames)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                continue;
            }

            var type = ResolveType(typeName);
            if (!typeof(Interceptor).IsAssignableFrom(type))
            {
                throw new PolymerConfigurationException(
                    $"Configured gRPC server interceptor '{typeName}' does not derive from {nameof(Interceptor)}.");
            }

            resolved.Add(type);
        }

        return resolved;
    }

    private GrpcServerTlsOptions? BuildGrpcServerTlsOptions(GrpcServerTlsConfiguration configuration)
    {
        if (configuration is null || string.IsNullOrWhiteSpace(configuration.CertificatePath))
        {
            return null;
        }

        var path = ResolvePath(configuration.CertificatePath!);
        if (!File.Exists(path))
        {
            throw new PolymerConfigurationException($"gRPC server certificate '{path}' could not be found.");
        }

        X509Certificate2 certificate;
#pragma warning disable SYSLIB0057
        certificate = string.IsNullOrEmpty(configuration.CertificatePassword)
            ? new X509Certificate2(path)
            : new X509Certificate2(path, configuration.CertificatePassword);
#pragma warning restore SYSLIB0057

        var mode = ParseClientCertificateMode(configuration.ClientCertificateMode);

        return new GrpcServerTlsOptions
        {
            Certificate = certificate,
            ClientCertificateMode = mode,
            CheckCertificateRevocation = configuration.CheckCertificateRevocation
        };
    }

    private GrpcTelemetryOptions? BuildGrpcTelemetryOptions(GrpcTelemetryConfiguration configuration, bool serverSide)
    {
        if (configuration is null)
        {
            return null;
        }

        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        var hasValues = configuration.EnableClientLogging.HasValue || configuration.EnableServerLogging.HasValue || loggerFactory is not null;

        if (!hasValues)
        {
            return null;
        }

        return new GrpcTelemetryOptions
        {
            EnableClientLogging = configuration.EnableClientLogging ?? !serverSide,
            EnableServerLogging = configuration.EnableServerLogging ?? serverSide,
            LoggerFactory = loggerFactory
        };
    }

    private void ApplyMiddleware(DispatcherOptions dispatcherOptions)
    {
        if (_options.Middleware is null)
        {
            return;
        }

        var inbound = _options.Middleware.Inbound;
        var outbound = _options.Middleware.Outbound;

        AddMiddleware(inbound.Unary, dispatcherOptions.UnaryInboundMiddleware);
        AddMiddleware(inbound.Oneway, dispatcherOptions.OnewayInboundMiddleware);
        AddMiddleware(inbound.Stream, dispatcherOptions.StreamInboundMiddleware);
        AddMiddleware(inbound.ClientStream, dispatcherOptions.ClientStreamInboundMiddleware);
        AddMiddleware(inbound.Duplex, dispatcherOptions.DuplexInboundMiddleware);

        AddMiddleware(outbound.Unary, dispatcherOptions.UnaryOutboundMiddleware);
        AddMiddleware(outbound.Oneway, dispatcherOptions.OnewayOutboundMiddleware);
        AddMiddleware(outbound.Stream, dispatcherOptions.StreamOutboundMiddleware);
        AddMiddleware(outbound.ClientStream, dispatcherOptions.ClientStreamOutboundMiddleware);
        AddMiddleware(outbound.Duplex, dispatcherOptions.DuplexOutboundMiddleware);
    }

    private void AddMiddleware<TMiddleware>(IEnumerable<string> typeNames, IList<TMiddleware> targetList)
    {
        foreach (var typeName in typeNames)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                continue;
            }

            var type = ResolveType(typeName);
            if (!typeof(TMiddleware).IsAssignableFrom(type))
            {
                throw new PolymerConfigurationException(
                    $"Configured middleware '{typeName}' does not implement {typeof(TMiddleware).Name}.");
            }

            var instance = (TMiddleware)ActivatorUtilities.CreateInstance(_serviceProvider, type);
            targetList.Add(instance);
        }
    }

    private static ClientCertificateMode ParseClientCertificateMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ClientCertificateMode.NoCertificate;
        }

        if (Enum.TryParse<ClientCertificateMode>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new PolymerConfigurationException(
            $"Unsupported client certificate mode '{value}'. Supported values: NoCertificate, AllowCertificate, RequireCertificate, RequireCertificateAndVerify.");
    }

    private static string ValidateHttpUrl(string value, string context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PolymerConfigurationException($"The url for {context} cannot be empty.");
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new PolymerConfigurationException($"The url '{value}' for {context} is not a valid HTTP/HTTPS address.");
        }

        return uri.ToString();
    }

    private static string ValidateGrpcUrl(string value, string context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PolymerConfigurationException($"The url/address for {context} cannot be empty.");
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        if (Uri.TryCreate($"http://{value}", UriKind.Absolute, out var fallback))
        {
            return fallback.ToString();
        }

        throw new PolymerConfigurationException($"The value '{value}' for {context} is not a valid URI.");
    }

    private static Type ResolveType(string typeName)
    {
        var resolved = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
        if (resolved is not null)
        {
            return resolved;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            resolved = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        throw new PolymerConfigurationException($"Type '{typeName}' could not be resolved. Ensure the assembly is loaded and the type name is fully qualified.");
    }

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));

    private enum OutboundKind
    {
        Unary,
        Oneway,
        Stream,
        ClientStream,
        Duplex
    }
}
