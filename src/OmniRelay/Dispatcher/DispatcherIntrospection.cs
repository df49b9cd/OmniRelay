using System.Collections.Immutable;

namespace OmniRelay.Dispatcher;

/// <summary>
/// Snapshot of dispatcher configuration and runtime bindings for diagnostics and introspection endpoints.
/// </summary>
public sealed record DispatcherIntrospection(
    string Service,
    DispatcherStatus Status,
    ProcedureGroups Procedures,
    ImmutableArray<LifecycleComponentDescriptor> Components,
    ImmutableArray<OutboundDescriptor> Outbounds,
    MiddlewareSummary Middleware);

/// <summary>Groups of registered procedures by RPC shape.</summary>
public sealed record ProcedureGroups(
    ImmutableArray<ProcedureDescriptor> Unary,
    ImmutableArray<ProcedureDescriptor> Oneway,
    ImmutableArray<StreamProcedureDescriptor> Stream,
    ImmutableArray<ClientStreamProcedureDescriptor> ClientStream,
    ImmutableArray<DuplexProcedureDescriptor> Duplex);

/// <summary>Basic procedure info including name, encoding, and aliases.</summary>
public sealed record ProcedureDescriptor(string Name, string? Encoding, ImmutableArray<string> Aliases);

/// <summary>Server-stream procedure descriptor with response metadata.</summary>
public sealed record StreamProcedureDescriptor(string Name, string? Encoding, ImmutableArray<string> Aliases, StreamIntrospectionMetadata Metadata);

/// <summary>Client-stream procedure descriptor with request metadata.</summary>
public sealed record ClientStreamProcedureDescriptor(string Name, string? Encoding, ImmutableArray<string> Aliases, ClientStreamIntrospectionMetadata Metadata);

/// <summary>Duplex-stream procedure descriptor with channel metadata.</summary>
public sealed record DuplexProcedureDescriptor(string Name, string? Encoding, ImmutableArray<string> Aliases, DuplexIntrospectionMetadata Metadata);

/// <summary>Lifecycle component descriptor including name and implementation type.</summary>
public sealed record LifecycleComponentDescriptor(string Name, string ComponentType);

/// <summary>Outbound binding descriptor lists transports per RPC shape for a service.</summary>
public sealed record OutboundDescriptor(
    string Service,
    ImmutableArray<OutboundBindingDescriptor> Unary,
    ImmutableArray<OutboundBindingDescriptor> Oneway,
    ImmutableArray<OutboundBindingDescriptor> Stream,
    ImmutableArray<OutboundBindingDescriptor> ClientStream,
    ImmutableArray<OutboundBindingDescriptor> Duplex);

/// <summary>Outbound transport binding including key, implementation type, and metadata.</summary>
public sealed record OutboundBindingDescriptor(string Key, string ImplementationType, object? Metadata);

/// <summary>Lists inbound and outbound middleware types by RPC shape.</summary>
public sealed record MiddlewareSummary(
    ImmutableArray<string> InboundUnary,
    ImmutableArray<string> InboundOneway,
    ImmutableArray<string> InboundStream,
    ImmutableArray<string> InboundClientStream,
    ImmutableArray<string> InboundDuplex,
    ImmutableArray<string> OutboundUnary,
    ImmutableArray<string> OutboundOneway,
    ImmutableArray<string> OutboundStream,
    ImmutableArray<string> OutboundClientStream,
    ImmutableArray<string> OutboundDuplex);
