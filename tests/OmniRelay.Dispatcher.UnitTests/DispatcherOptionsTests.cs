using AwesomeAssertions;
using NSubstitute;
using OmniRelay.Core.Transport;
using Xunit;
using static AwesomeAssertions.FluentActions;

namespace OmniRelay.Dispatcher.UnitTests;

public class DispatcherOptionsTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public void Constructor_WithBlankServiceName_Throws()
    {
        Invoking(() => new DispatcherOptions("  "))
            .Should().Throw<ArgumentException>();
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public async ValueTask AddLifecycle_WithDuplicateInstance_StartsOnce()
    {
        var options = new DispatcherOptions("test-service");
        var lifecycle = new CountingLifecycle();

        options.AddLifecycle("first", lifecycle);
        options.AddLifecycle("second", lifecycle);

        var dispatcher = new Dispatcher(options);

        await dispatcher.StartAsyncChecked(CancellationToken.None);
        await dispatcher.StopAsyncChecked(CancellationToken.None);

        lifecycle.StartCalls.Should().Be(1);
        lifecycle.StopCalls.Should().Be(1);
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void AddTransport_AddsLifecycleComponent()
    {
        var options = new DispatcherOptions("svc");
        var transport = Substitute.For<ITransport>();
        transport.Name.Returns("http");
        options.AddTransport(transport);

        var dispatcher = new Dispatcher(options);
        var components = dispatcher.Introspect().Components;

        components.Should().Contain(component => component.Name == "http");
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void AddUnaryOutbound_RegistersLifecycle()
    {
        var options = new DispatcherOptions("svc");
        options.AddUnaryOutbound("remote", null, Substitute.For<IUnaryOutbound>());

        var dispatcher = new Dispatcher(options);
        var outbounds = dispatcher.Introspect().Outbounds.Single();

        outbounds.Service.Should().Be("remote");
        outbounds.Unary.Should().HaveCount(1);
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void AddUnaryOutbound_TrimsKeys()
    {
        var options = new DispatcherOptions("svc");
        var outbound = Substitute.For<IUnaryOutbound>();
        options.AddUnaryOutbound("remote", "  primary  ", outbound);

        var dispatcher = new Dispatcher(options);
        var config = dispatcher.ClientConfigChecked("remote");

        config.TryGetUnary("primary", out var resolved).Should().BeTrue();
        resolved.Should().BeSameAs(outbound);
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void AddUnaryOutbound_DuplicateTrimmedKey_Throws()
    {
        var options = new DispatcherOptions("svc");
        options.AddUnaryOutbound("remote", "primary", Substitute.For<IUnaryOutbound>());

        Invoking(() =>
                options.AddUnaryOutbound("remote", " primary ", Substitute.For<IUnaryOutbound>()))
            .Should().Throw<InvalidOperationException>();
    }

    private sealed class CountingLifecycle : ILifecycle
    {
        private int _startCalls;
        private int _stopCalls;

        public int StartCalls => _startCalls;
        public int StopCalls => _stopCalls;

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _startCalls);
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _stopCalls);
            return ValueTask.CompletedTask;
        }
    }
}
