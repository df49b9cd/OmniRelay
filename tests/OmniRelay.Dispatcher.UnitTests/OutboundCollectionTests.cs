using System.Collections.Immutable;
using AwesomeAssertions;
using NSubstitute;
using OmniRelay.Core.Transport;
using Xunit;

namespace OmniRelay.Dispatcher.UnitTests;

public class OutboundRegistryTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public void Resolve_WithNullKey_ReturnsDefaultBinding()
    {
        var unary = Substitute.For<IUnaryOutbound>();
        var collection = CreateCollection(unaryOutbound: unary);

        collection.ResolveUnary().Should().BeSameAs(unary);
        collection.ResolveUnary(" ").Should().BeSameAs(unary);
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void Resolve_WithAlternateKey_IsCaseInsensitive()
    {
        var unary = Substitute.For<IUnaryOutbound>();
        var map = ImmutableDictionary.Create<string, IUnaryOutbound>(StringComparer.OrdinalIgnoreCase)
            .Add(OutboundRegistry.DefaultKey, Substitute.For<IUnaryOutbound>())
            .Add("primary", unary);

        var collection = new OutboundRegistry(
            "downstream",
            map,
            [],
            [],
            [],
            []);

        collection.ResolveUnary("PRIMARY").Should().BeSameAs(unary);
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void TryGet_ReturnsFalseWhenKeyMissing()
    {
        var collection = CreateCollection();

        collection.TryGetUnary("missing", out _).Should().BeFalse();
        collection.TryGetOneway("missing", out _).Should().BeFalse();
        collection.TryGetStream("missing", out _).Should().BeFalse();
        collection.TryGetClientStream("missing", out _).Should().BeFalse();
        collection.TryGetDuplex("missing", out _).Should().BeFalse();
    }

    private static OutboundRegistry CreateCollection(
        IUnaryOutbound? unaryOutbound = null)
    {
        unaryOutbound ??= Substitute.For<IUnaryOutbound>();

        return new OutboundRegistry(
            "downstream",
            ImmutableDictionary<string, IUnaryOutbound>.Empty.Add(OutboundRegistry.DefaultKey, unaryOutbound),
            ImmutableDictionary<string, IOnewayOutbound>.Empty.Add(OutboundRegistry.DefaultKey, Substitute.For<IOnewayOutbound>()),
            ImmutableDictionary<string, IStreamOutbound>.Empty.Add(OutboundRegistry.DefaultKey, Substitute.For<IStreamOutbound>()),
            ImmutableDictionary<string, IClientStreamOutbound>.Empty.Add(OutboundRegistry.DefaultKey, Substitute.For<IClientStreamOutbound>()),
            ImmutableDictionary<string, IDuplexOutbound>.Empty.Add(OutboundRegistry.DefaultKey, Substitute.For<IDuplexOutbound>()));
    }
}
