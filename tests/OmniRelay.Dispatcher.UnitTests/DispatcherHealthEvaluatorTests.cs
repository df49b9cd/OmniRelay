using System.Collections.Immutable;
using AwesomeAssertions;
using NSubstitute;
using OmniRelay.Core.Transport;
using OmniRelay.Transport.Grpc;
using Xunit;

namespace OmniRelay.Dispatcher.UnitTests;

public class DispatcherHealthEvaluatorTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public void Evaluate_WhenDispatcherNotRunning_ReportsStatusIssue()
    {
        var dispatcher = new Dispatcher(new DispatcherOptions("svc"));

        var result = DispatcherHealthEvaluator.Evaluate(dispatcher);

        result.IsReady.Should().BeFalse();
        result.Issues.Should().Contain("dispatcher-status:Created");
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public async ValueTask Evaluate_WithGrpcOutboundReportsIssues()
    {
        var initial = await EvaluateAsync(CreateSnapshot(isStarted: false, peers: []));
        initial.Issues.Should().Contain("grpc:remote:unary:default:not-started");

        var noPeers = await EvaluateAsync(CreateSnapshot(isStarted: true, peers: []));
        noPeers.Issues.Should().Contain("grpc:remote:unary:default:no-peers");

        var noAvailable = await EvaluateAsync(CreateSnapshot(
            isStarted: true,
            peers: [new Uri("http://localhost")],
            summaries: [new GrpcPeerSummary(new Uri("http://localhost"), Core.Peers.PeerState.Unavailable, 0, null, null, 0, 0, null, null, null, null)
            ]));
        noAvailable.Issues.Should().Contain("grpc:remote:unary:default:no-available-peers");

        var healthy = await EvaluateAsync(CreateSnapshot(
            isStarted: true,
            peers: [new Uri("http://localhost")],
            summaries: [new GrpcPeerSummary(new Uri("http://localhost"), Core.Peers.PeerState.Available, 0, null, null, 0, 0, null, null, null, null)
            ]));
        healthy.IsReady.Should().BeTrue();
        healthy.Issues.Should().BeEmpty();
    }

    private static async Task<DispatcherReadinessResult> EvaluateAsync(GrpcOutboundSnapshot snapshot)
    {
        var options = new DispatcherOptions("svc");
        var outbound = Substitute.For<IUnaryOutbound, IOutboundDiagnostic>();
        ((IOutboundDiagnostic)outbound).GetOutboundDiagnostics().Returns(snapshot);
        options.AddUnaryOutbound("remote", null, outbound);
        var dispatcher = new Dispatcher(options);
        await dispatcher.StartAsyncChecked();
        try
        {
            return DispatcherHealthEvaluator.Evaluate(dispatcher);
        }
        finally
        {
            await dispatcher.StopAsyncChecked();
        }
    }

    private static GrpcOutboundSnapshot CreateSnapshot(
        bool isStarted,
        IReadOnlyList<Uri> peers,
        ImmutableArray<GrpcPeerSummary>? summaries = null)
    {
        summaries ??= [];
        return new GrpcOutboundSnapshot(
            "remote",
            peers,
            "round-robin",
            isStarted,
            summaries.Value,
            []);
    }
}
