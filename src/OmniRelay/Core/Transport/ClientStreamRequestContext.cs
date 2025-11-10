using System.Threading.Channels;

namespace OmniRelay.Core.Transport;

/// <summary>
/// Provides access to request metadata and the raw request message reader for client-streaming handlers.
/// </summary>
public readonly struct ClientStreamRequestContext(RequestMeta meta, ChannelReader<ReadOnlyMemory<byte>> requests)
{
    /// <summary>Gets the request metadata.</summary>
    public RequestMeta Meta
    {
        get => field;
    } = meta;

    /// <summary>Gets the raw request message reader.</summary>
    public ChannelReader<ReadOnlyMemory<byte>> Requests
    {
        get => field;
    } = requests;
}
