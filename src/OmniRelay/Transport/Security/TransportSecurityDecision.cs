namespace OmniRelay.Transport.Security;

/// <summary>Result of applying the transport security policy to a request.</summary>
public readonly record struct TransportSecurityDecision(bool IsAllowed, string? Reason)
{
    public static TransportSecurityDecision Allowed { get; } = new(true, null);

    public object ToPayload(string transport, string endpoint) => new
    {
        allowed = IsAllowed,
        transport,
        endpoint,
        reason = Reason
    };
}
