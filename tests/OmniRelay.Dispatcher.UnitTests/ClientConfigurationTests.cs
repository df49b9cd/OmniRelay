using System.Collections.Immutable;
using AwesomeAssertions;
using NSubstitute;
using OmniRelay.Core.Middleware;
using OmniRelay.Core.Transport;
using Xunit;

namespace OmniRelay.Dispatcher.UnitTests;

public class ClientConfigurationTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public void Service_ReturnsOutboundService()
    {
        var config = CreateConfiguration(out var unaryOutbound);

        config.Service.Should().Be("downstream");
        config.ResolveUnary().Should().BeSameAs(unaryOutbound);
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void Resolve_WithUnknownKey_ReturnsNull()
    {
        var config = CreateConfiguration(out _);

        config.ResolveOneway("missing").Should().BeNull();
        config.ResolveStream("missing").Should().BeNull();
        config.ResolveClientStream("missing").Should().BeNull();
        config.ResolveDuplex("missing").Should().BeNull();
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void TryGet_ReturnsFalseWhenOutboundNotFound()
    {
        var config = CreateConfiguration(out _);

        config.TryGetUnary("missing", out _).Should().BeFalse();
        config.TryGetOneway("missing", out _).Should().BeFalse();
        config.TryGetStream("missing", out _).Should().BeFalse();
        config.TryGetClientStream("missing", out _).Should().BeFalse();
        config.TryGetDuplex("missing", out _).Should().BeFalse();
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void Middleware_CollectionsExposeConfiguredInstances()
    {
        var unaryMiddleware = ImmutableArray.Create(Substitute.For<IUnaryOutboundMiddleware>());
        var onewayMiddleware = ImmutableArray.Create(Substitute.For<IOnewayOutboundMiddleware>());
        var streamMiddleware = ImmutableArray.Create(Substitute.For<IStreamOutboundMiddleware>());
        var clientStreamMiddleware = ImmutableArray.Create(Substitute.For<IClientStreamOutboundMiddleware>());
        var duplexMiddleware = ImmutableArray.Create(Substitute.For<IDuplexOutboundMiddleware>());

        var collection = new OutboundRegistry(
            "downstream",
            [],
            [],
            [],
            [],
            []);

        var config = new ClientConfiguration(
            collection,
            unaryMiddleware,
            onewayMiddleware,
            streamMiddleware,
            clientStreamMiddleware,
            duplexMiddleware);

        config.UnaryMiddleware[0].Should().BeSameAs(unaryMiddleware[0]);
        config.OnewayMiddleware[0].Should().BeSameAs(onewayMiddleware[0]);
        config.StreamMiddleware[0].Should().BeSameAs(streamMiddleware[0]);
        config.ClientStreamMiddleware[0].Should().BeSameAs(clientStreamMiddleware[0]);
        config.DuplexMiddleware[0].Should().BeSameAs(duplexMiddleware[0]);
    }

    private static ClientConfiguration CreateConfiguration(out IUnaryOutbound unaryOutbound)
    {
        unaryOutbound = Substitute.For<IUnaryOutbound>();
        var onewayOutbound = Substitute.For<IOnewayOutbound>();
        var streamOutbound = Substitute.For<IStreamOutbound>();
        var clientStreamOutbound = Substitute.For<IClientStreamOutbound>();
        var duplexOutbound = Substitute.For<IDuplexOutbound>();

        var collection = new OutboundRegistry(
            "downstream",
            ImmutableDictionary<string, IUnaryOutbound>.Empty.Add(OutboundRegistry.DefaultKey, unaryOutbound),
            ImmutableDictionary<string, IOnewayOutbound>.Empty.Add(OutboundRegistry.DefaultKey, onewayOutbound),
            ImmutableDictionary<string, IStreamOutbound>.Empty.Add(OutboundRegistry.DefaultKey, streamOutbound),
            ImmutableDictionary<string, IClientStreamOutbound>.Empty.Add(OutboundRegistry.DefaultKey, clientStreamOutbound),
            ImmutableDictionary<string, IDuplexOutbound>.Empty.Add(OutboundRegistry.DefaultKey, duplexOutbound));

        return new ClientConfiguration(
            collection,
            [],
            [],
            [],
            [],
            []);
    }
}
