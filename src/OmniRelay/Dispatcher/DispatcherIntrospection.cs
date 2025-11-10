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
    MiddlewareSummary Middleware)
{
    public string Service
    {
        get => field;
        init => field = value;
    } = Service;

    public DispatcherStatus Status
    {
        get => field;
        init => field = value;
    } = Status;

    public ProcedureGroups Procedures
    {
        get => field;
        init => field = value;
    } = Procedures;

    public ImmutableArray<LifecycleComponentDescriptor> Components
    {
        get => field;
        init => field = value;
    } = Components;

    public ImmutableArray<OutboundDescriptor> Outbounds
    {
        get => field;
        init => field = value;
    } = Outbounds;

    public MiddlewareSummary Middleware
    {
        get => field;
        init => field = value;
    } = Middleware;
}

/// <summary>Groups of registered procedures by RPC shape.</summary>
public sealed record ProcedureGroups(
    ImmutableArray<ProcedureDescriptor> Unary,
    ImmutableArray<ProcedureDescriptor> Oneway,
    ImmutableArray<StreamProcedureDescriptor> Stream,
    ImmutableArray<ClientStreamProcedureDescriptor> ClientStream,
    ImmutableArray<DuplexProcedureDescriptor> Duplex)
{
    public ImmutableArray<ProcedureDescriptor> Unary
    {
        get => field;
        init => field = value;
    } = Unary;

    public ImmutableArray<ProcedureDescriptor> Oneway
    {
        get => field;
        init => field = value;
    } = Oneway;

    public ImmutableArray<StreamProcedureDescriptor> Stream
    {
        get => field;
        init => field = value;
    } = Stream;

    public ImmutableArray<ClientStreamProcedureDescriptor> ClientStream
    {
        get => field;
        init => field = value;
    } = ClientStream;

    public ImmutableArray<DuplexProcedureDescriptor> Duplex
    {
        get => field;
        init => field = value;
    } = Duplex;
}

/// <summary>Basic procedure info including name, encoding, and aliases.</summary>
public sealed record ProcedureDescriptor(string Name, string? Encoding, ImmutableArray<string> Aliases)
{
    public string Name
    {
        get => field;
        init => field = value;
    } = Name;

    public string? Encoding
    {
        get => field;
        init => field = value;
    } = Encoding;

    public ImmutableArray<string> Aliases
    {
        get => field;
        init => field = value;
    } = Aliases;
}

/// <summary>Server-stream procedure descriptor with response metadata.</summary>
public sealed record StreamProcedureDescriptor(string Name, string? Encoding, ImmutableArray<string> Aliases, StreamIntrospectionMetadata Metadata)
{
    public string Name
    {
        get => field;
        init => field = value;
    } = Name;

    public string? Encoding
    {
        get => field;
        init => field = value;
    } = Encoding;

    public ImmutableArray<string> Aliases
    {
        get => field;
        init => field = value;
    } = Aliases;

    public StreamIntrospectionMetadata Metadata
    {
        get => field;
        init => field = value;
    } = Metadata;
}

/// <summary>Client-stream procedure descriptor with request metadata.</summary>
public sealed record ClientStreamProcedureDescriptor(string Name, string? Encoding, ImmutableArray<string> Aliases, ClientStreamIntrospectionMetadata Metadata)
{
    public string Name
    {
        get => field;
        init => field = value;
    } = Name;

    public string? Encoding
    {
        get => field;
        init => field = value;
    } = Encoding;

    public ImmutableArray<string> Aliases
    {
        get => field;
        init => field = value;
    } = Aliases;

    public ClientStreamIntrospectionMetadata Metadata
    {
        get => field;
        init => field = value;
    } = Metadata;
}

/// <summary>Duplex-stream procedure descriptor with channel metadata.</summary>
public sealed record DuplexProcedureDescriptor(string Name, string? Encoding, ImmutableArray<string> Aliases, DuplexIntrospectionMetadata Metadata)
{
    public string Name
    {
        get => field;
        init => field = value;
    } = Name;

    public string? Encoding
    {
        get => field;
        init => field = value;
    } = Encoding;

    public ImmutableArray<string> Aliases
    {
        get => field;
        init => field = value;
    } = Aliases;

    public DuplexIntrospectionMetadata Metadata
    {
        get => field;
        init => field = value;
    } = Metadata;
}

/// <summary>Lifecycle component descriptor including name and implementation type.</summary>
public sealed record LifecycleComponentDescriptor(string Name, string ComponentType)
{
    public string Name
    {
        get => field;
        init => field = value;
    } = Name;

    public string ComponentType
    {
        get => field;
        init => field = value;
    } = ComponentType;
}

/// <summary>Outbound binding descriptor lists transports per RPC shape for a service.</summary>
public sealed record OutboundDescriptor(
    string Service,
    ImmutableArray<OutboundBindingDescriptor> Unary,
    ImmutableArray<OutboundBindingDescriptor> Oneway,
    ImmutableArray<OutboundBindingDescriptor> Stream,
    ImmutableArray<OutboundBindingDescriptor> ClientStream,
    ImmutableArray<OutboundBindingDescriptor> Duplex)
{
    public string Service
    {
        get => field;
        init => field = value;
    } = Service;

    public ImmutableArray<OutboundBindingDescriptor> Unary
    {
        get => field;
        init => field = value;
    } = Unary;

    public ImmutableArray<OutboundBindingDescriptor> Oneway
    {
        get => field;
        init => field = value;
    } = Oneway;

    public ImmutableArray<OutboundBindingDescriptor> Stream
    {
        get => field;
        init => field = value;
    } = Stream;

    public ImmutableArray<OutboundBindingDescriptor> ClientStream
    {
        get => field;
        init => field = value;
    } = ClientStream;

    public ImmutableArray<OutboundBindingDescriptor> Duplex
    {
        get => field;
        init => field = value;
    } = Duplex;
}

/// <summary>Outbound transport binding including key, implementation type, and metadata.</summary>
public sealed record OutboundBindingDescriptor(string Key, string ImplementationType, object? Metadata)
{
    public string Key
    {
        get => field;
        init => field = value;
    } = Key;

    public string ImplementationType
    {
        get => field;
        init => field = value;
    } = ImplementationType;

    public object? Metadata
    {
        get => field;
        init => field = value;
    } = Metadata;
}

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
    ImmutableArray<string> OutboundDuplex)
{
    public ImmutableArray<string> InboundUnary
    {
        get => field;
        init => field = value;
    } = InboundUnary;

    public ImmutableArray<string> InboundOneway
    {
        get => field;
        init => field = value;
    } = InboundOneway;

    public ImmutableArray<string> InboundStream
    {
        get => field;
        init => field = value;
    } = InboundStream;

    public ImmutableArray<string> InboundClientStream
    {
        get => field;
        init => field = value;
    } = InboundClientStream;

    public ImmutableArray<string> InboundDuplex
    {
        get => field;
        init => field = value;
    } = InboundDuplex;

    public ImmutableArray<string> OutboundUnary
    {
        get => field;
        init => field = value;
    } = OutboundUnary;

    public ImmutableArray<string> OutboundOneway
    {
        get => field;
        init => field = value;
    } = OutboundOneway;

    public ImmutableArray<string> OutboundStream
    {
        get => field;
        init => field = value;
    } = OutboundStream;

    public ImmutableArray<string> OutboundClientStream
    {
        get => field;
        init => field = value;
    } = OutboundClientStream;

    public ImmutableArray<string> OutboundDuplex
    {
        get => field;
        init => field = value;
    } = OutboundDuplex;
}
