using AwesomeAssertions;
using OmniRelay.Core.Transport;
using Xunit;

namespace OmniRelay.Dispatcher.UnitTests;

public class StreamingIntrospectionMetadataTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public void StreamChannelMetadata_DefaultsMatchExpected()
    {
        var response = StreamChannelMetadata.DefaultResponse;
        response.Direction.Should().Be(StreamDirection.Server);
        response.Capacity.Should().BeNull();
        response.TracksMessageCount.Should().BeTrue();

        var request = StreamChannelMetadata.DefaultRequest;
        request.Direction.Should().Be(StreamDirection.Client);
        request.TracksMessageCount.Should().BeFalse();
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void AggregateDefaults_ComposeChannelMetadata()
    {
        StreamIntrospectionMetadata.Default.ResponseChannel.Should().Be(StreamChannelMetadata.DefaultResponse);
        ClientStreamIntrospectionMetadata.Default.RequestChannel.Should().Be(StreamChannelMetadata.DefaultRequest);
        ClientStreamIntrospectionMetadata.Default.AggregatesUnaryResponse.Should().BeTrue();

        var duplex = DuplexIntrospectionMetadata.Default;
        duplex.RequestChannel.Should().Be(StreamChannelMetadata.DefaultRequest);
        duplex.ResponseChannel.Should().Be(StreamChannelMetadata.DefaultResponse);
    }
}
