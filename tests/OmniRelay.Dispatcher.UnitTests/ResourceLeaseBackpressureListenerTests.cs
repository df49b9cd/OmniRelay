using System.Threading.RateLimiting;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OmniRelay.ControlPlane.Throttling;
using OmniRelay.Core;
using Xunit;

namespace OmniRelay.Dispatcher.UnitTests;

public sealed class ResourceLeaseBackpressureListenerTests
{
    private static ResourceLeaseBackpressureSignal ActiveSignal(long pending = 10) =>
        new(true, pending, DateTimeOffset.UtcNow, HighWatermark: 8, LowWatermark: 4);

    private static ResourceLeaseBackpressureSignal ClearedSignal(long pending = 2) =>
        new(false, pending, DateTimeOffset.UtcNow, HighWatermark: 8, LowWatermark: 4);

    [Fact(Timeout = TestTimeouts.Default)]
    public async ValueTask RateLimitingListener_TogglesGate()
    {
        await using var normal = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = 16,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        await using var throttled = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = 2,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        var gate = new BackpressureAwareRateLimiter(normalLimiter: normal, backpressureLimiter: throttled);
        var logger = Substitute.For<ILogger>();
        var listener = new RateLimitingBackpressureListener(gate, logger);
        var meta = new RequestMeta(service: "svc", transport: "http");

        gate.SelectLimiter(meta).Should().BeSameAs(normal);

        await listener.OnBackpressureChanged(ActiveSignal(), CancellationToken.None);
        gate.SelectLimiter(meta).Should().BeSameAs(throttled);

        await listener.OnBackpressureChanged(ClearedSignal(), CancellationToken.None);
        gate.SelectLimiter(meta).Should().BeSameAs(normal);
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public async ValueTask DiagnosticsListener_StoresLatestAndStreams()
    {
        var listener = new ResourceLeaseBackpressureDiagnosticsListener(historyCapacity: 4);
        listener.Latest.Should().BeNull();

        var signal = ActiveSignal(pending: 42);
        await listener.OnBackpressureChanged(signal, CancellationToken.None);

        listener.Latest.Should().Be(signal);

        var read = await listener.Updates.ReadAsync(CancellationToken.None);
        read.Should().Be(signal);

        var cleared = ClearedSignal();
        await listener.OnBackpressureChanged(cleared, CancellationToken.None);
        listener.Latest.Should().Be(cleared);

        listener.Updates.TryRead(out var clearedRead).Should().BeTrue();
        clearedRead.Should().Be(cleared);
    }
}
