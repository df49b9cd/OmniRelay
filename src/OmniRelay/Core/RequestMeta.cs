using System.Collections.Immutable;

namespace OmniRelay.Core;

/// <summary>
/// Carries transport-agnostic metadata for an RPC request.
/// </summary>
public sealed record RequestMeta
{
    private static readonly ImmutableDictionary<string, string> EmptyHeaders =
        ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase);

    public string Service
    {
        get => field;
        init => field = value;
    } = string.Empty;

    public string? Procedure
    {
        get => field;
        init => field = value;
    }

    public string? Caller
    {
        get => field;
        init => field = value;
    }

    public string? Encoding
    {
        get => field;
        init => field = value;
    }

    public string? Transport
    {
        get => field;
        init => field = value;
    }

    public string? ShardKey
    {
        get => field;
        init => field = value;
    }

    public string? RoutingKey
    {
        get => field;
        init => field = value;
    }

    public string? RoutingDelegate
    {
        get => field;
        init => field = value;
    }

    public TimeSpan? TimeToLive
    {
        get => field;
        init => field = value;
    }

    public DateTimeOffset? Deadline
    {
        get => field;
        init => field = value;
    }

    public ImmutableDictionary<string, string> Headers
    {
        get => field;
        init => field = value;
    } = EmptyHeaders;

    public RequestMeta()
    {
    }

    public RequestMeta(
        string service,
        string? procedure = null,
        string? caller = null,
        string? encoding = null,
        string? transport = null,
        string? shardKey = null,
        string? routingKey = null,
        string? routingDelegate = null,
        TimeSpan? timeToLive = null,
        DateTimeOffset? deadline = null,
        IEnumerable<KeyValuePair<string, string>>? headers = null)
    {
        Service = service ?? string.Empty;
        Procedure = procedure;
        Caller = caller;
        Encoding = encoding;
        Transport = transport;
        ShardKey = shardKey;
        RoutingKey = routingKey;
        RoutingDelegate = routingDelegate;
        TimeToLive = timeToLive;
        Deadline = deadline;
        Headers = headers is null
            ? EmptyHeaders
            : ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, headers) ?? EmptyHeaders;
    }

    public RequestMeta WithHeader(string key, string value)
    {
        var updated = Headers.SetItem(key, value);
#pragma warning disable CS8601
        return CopyWithHeaders(updated);
#pragma warning restore CS8601
    }

    public RequestMeta WithHeaders(IEnumerable<KeyValuePair<string, string>> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        builder.AddRange(Headers);

        foreach (var kvp in headers)
        {
            builder[kvp.Key] = kvp.Value;
        }

        var merged = builder.ToImmutable();
#pragma warning disable CS8601
        return CopyWithHeaders(merged);
#pragma warning restore CS8601
    }

    private RequestMeta CopyWithHeaders(ImmutableDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        return new RequestMeta(
            service: Service,
            procedure: Procedure,
            caller: Caller,
            encoding: Encoding,
            transport: Transport,
            shardKey: ShardKey,
            routingKey: RoutingKey,
            routingDelegate: RoutingDelegate,
            timeToLive: TimeToLive,
            deadline: Deadline,
            headers: headers);
    }

    public bool TryGetHeader(string key, out string? value) =>
        Headers.TryGetValue(key, out value);
}
