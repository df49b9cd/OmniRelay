using System.Threading.Channels;

namespace OmniRelay.Core.Transport;

public readonly struct ClientStreamRequestContext(RequestMeta meta, ChannelReader<ReadOnlyMemory<byte>> requests)
{
    public RequestMeta Meta { get; } = meta;
    public ChannelReader<ReadOnlyMemory<byte>> Requests { get; } = requests;
}
