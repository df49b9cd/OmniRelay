using OmniRelay.Core.Transport;

namespace OmniRelay.Dispatcher;

public sealed record StreamChannelMetadata(
    StreamDirection Direction,
    string BufferingStrategy,
    int? Capacity,
    bool TracksMessageCount)
{
    public static StreamChannelMetadata DefaultResponse
    {
        get => field;
    } =
        new(StreamDirection.Server, "unbounded-channel", null, true);

    public static StreamChannelMetadata DefaultRequest
    {
        get => field;
    } =
        new(StreamDirection.Client, "unbounded-channel", null, false);

    public StreamDirection Direction
    {
        get => field;
        init => field = value;
    } = Direction;

    public string BufferingStrategy
    {
        get => field;
        init => field = value;
    } = BufferingStrategy;

    public int? Capacity
    {
        get => field;
        init => field = value;
    } = Capacity;

    public bool TracksMessageCount
    {
        get => field;
        init => field = value;
    } = TracksMessageCount;
}

public sealed record StreamIntrospectionMetadata(StreamChannelMetadata ResponseChannel)
{
    public static StreamIntrospectionMetadata Default
    {
        get => field;
    } =
        new(StreamChannelMetadata.DefaultResponse);

    public StreamChannelMetadata ResponseChannel
    {
        get => field;
        init => field = value;
    } = ResponseChannel;
}

public sealed record ClientStreamIntrospectionMetadata(
    StreamChannelMetadata RequestChannel,
    bool AggregatesUnaryResponse)
{
    public static ClientStreamIntrospectionMetadata Default
    {
        get => field;
    } =
        new(StreamChannelMetadata.DefaultRequest, true);

    public StreamChannelMetadata RequestChannel
    {
        get => field;
        init => field = value;
    } = RequestChannel;

    public bool AggregatesUnaryResponse
    {
        get => field;
        init => field = value;
    } = AggregatesUnaryResponse;
}

public sealed record DuplexIntrospectionMetadata(
    StreamChannelMetadata RequestChannel,
    StreamChannelMetadata ResponseChannel)
{
    public static DuplexIntrospectionMetadata Default
    {
        get => field;
    } =
        new(StreamChannelMetadata.DefaultRequest, StreamChannelMetadata.DefaultResponse);

    public StreamChannelMetadata RequestChannel
    {
        get => field;
        init => field = value;
    } = RequestChannel;

    public StreamChannelMetadata ResponseChannel
    {
        get => field;
        init => field = value;
    } = ResponseChannel;
}
