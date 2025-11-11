using Microsoft.Extensions.Logging.Nulls;
using OmniRelay.Core.Leadership;
using Xunit;

namespace OmniRelay.Core.UnitTests.Leadership;

public sealed class LeadershipEventHubTests
{
    [Fact]
    public async Task Subscribe_ReceivesEvents()
    {
        var hub = new LeadershipEventHub(NullLogger<LeadershipEventHub>.Instance);
        var receivedEvents = new List<LeadershipEvent>();

        using var subscription = hub.Subscribe(evt =>
        {
            receivedEvents.Add(evt);
            return ValueTask.CompletedTask;
        });

        var testEvent = new LeadershipEvent
        {
            Kind = LeadershipEventKind.Acquired,
            ScopeId = "test-scope",
            NodeId = "test-node",
            FenceToken = 1,
            Timestamp = DateTimeOffset.UtcNow
        };

        await hub.PublishAsync(testEvent);

        Assert.Single(receivedEvents);
        Assert.Equal(LeadershipEventKind.Acquired, receivedEvents[0].Kind);
        Assert.Equal("test-scope", receivedEvents[0].ScopeId);
        Assert.Equal("test-node", receivedEvents[0].NodeId);
    }

    [Fact]
    public async Task MultipleSubscribers_AllReceiveEvents()
    {
        var hub = new LeadershipEventHub(NullLogger<LeadershipEventHub>.Instance);
        var receivedEvents1 = new List<LeadershipEvent>();
        var receivedEvents2 = new List<LeadershipEvent>();

        using var subscription1 = hub.Subscribe(evt =>
        {
            receivedEvents1.Add(evt);
            return ValueTask.CompletedTask;
        });

        using var subscription2 = hub.Subscribe(evt =>
        {
            receivedEvents2.Add(evt);
            return ValueTask.CompletedTask;
        });

        var testEvent = new LeadershipEvent
        {
            Kind = LeadershipEventKind.Released,
            ScopeId = "scope",
            NodeId = "node",
            FenceToken = 2,
            Timestamp = DateTimeOffset.UtcNow
        };

        await hub.PublishAsync(testEvent);

        Assert.Single(receivedEvents1);
        Assert.Single(receivedEvents2);
        Assert.Equal(testEvent.ScopeId, receivedEvents1[0].ScopeId);
        Assert.Equal(testEvent.ScopeId, receivedEvents2[0].ScopeId);
    }

    [Fact]
    public async Task Unsubscribe_StopsReceivingEvents()
    {
        var hub = new LeadershipEventHub(NullLogger<LeadershipEventHub>.Instance);
        var receivedEvents = new List<LeadershipEvent>();

        var subscription = hub.Subscribe(evt =>
        {
            receivedEvents.Add(evt);
            return ValueTask.CompletedTask;
        });

        var event1 = new LeadershipEvent
        {
            Kind = LeadershipEventKind.Acquired,
            ScopeId = "scope",
            NodeId = "node",
            FenceToken = 1,
            Timestamp = DateTimeOffset.UtcNow
        };

        await hub.PublishAsync(event1);
        Assert.Single(receivedEvents);

        subscription.Dispose();

        var event2 = new LeadershipEvent
        {
            Kind = LeadershipEventKind.Released,
            ScopeId = "scope",
            NodeId = "node",
            FenceToken = 2,
            Timestamp = DateTimeOffset.UtcNow
        };

        await hub.PublishAsync(event2);
        Assert.Single(receivedEvents);
    }
}
