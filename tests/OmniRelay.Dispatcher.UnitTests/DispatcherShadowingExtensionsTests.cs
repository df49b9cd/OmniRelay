using AwesomeAssertions;
using NSubstitute;
using OmniRelay.Core.Transport;
using Xunit;

namespace OmniRelay.Dispatcher.UnitTests;

public class DispatcherShadowingExtensionsTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public void AddTeeUnaryOutbound_RegistersTeeOutbound()
    {
        var options = new DispatcherOptions("svc");
        options.AddTeeUnaryOutbound("downstream", null, Substitute.For<IUnaryOutbound>(), Substitute.For<IUnaryOutbound>());

        var dispatcher = new Dispatcher(options);
        var outbound = dispatcher.ClientConfigChecked("downstream").ResolveUnary();

        outbound.Should().BeOfType<TeeUnaryOutbound>();
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void AddTeeOnewayOutbound_RegistersTeeOutbound()
    {
        var options = new DispatcherOptions("svc");
        options.AddTeeOnewayOutbound("downstream", "shadow", Substitute.For<IOnewayOutbound>(), Substitute.For<IOnewayOutbound>());

        var dispatcher = new Dispatcher(options);
        var outbound = dispatcher.ClientConfigChecked("downstream").ResolveOneway("shadow");

        outbound.Should().BeOfType<TeeOnewayOutbound>();
    }
}
