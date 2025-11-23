using Hugo;
using OmniRelay.Dispatcher;
using static Hugo.Go;
using Xunit;

namespace OmniRelay.Tests.Dispatcher;

public class DispatcherLifecycleSpikeTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public async ValueTask RunAsync_CoordinatesStartAndStopSequences()
    {
        var startSteps = new List<Func<CancellationToken, ValueTask<Result<Unit>>>>
        {
            async ct =>
            {
                await Task.Delay(50, ct);
                return Ok(Unit.Value);
            },
            async ct =>
            {
                await Task.Delay(10, ct);
                return Ok(Unit.Value);
            }
        };

        var stopSteps = new List<Func<CancellationToken, ValueTask<Result<Unit>>>>
        {
            async ct =>
            {
                await Task.Delay(5, ct);
                return Ok(Unit.Value);
            },
            async ct =>
            {
                await Task.Delay(15, ct);
                return Ok(Unit.Value);
            }
        };

        var result = await DispatcherLifecycleSpike.RunAsync(
            startSteps,
            stopSteps,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);

        var (started, stopped) = result.Value;

        Assert.Equal(startSteps.Count, started.Count);
        Assert.All(Enumerable.Range(0, startSteps.Count), index =>
            Assert.Contains($"start:{index}", started));

        Assert.Equal(stopSteps.Count, stopped.Count);
        Assert.Equal(["stop:0", "stop:1"], stopped);
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public async ValueTask RunAsync_PropagatesCancellation()
    {
        var startSteps = new List<Func<CancellationToken, ValueTask<Result<Unit>>>>
        {
            async ct =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return Ok(Unit.Value);
            }
        };

        var stopSteps = new List<Func<CancellationToken, ValueTask<Result<Unit>>>>();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var result = await DispatcherLifecycleSpike.RunAsync(startSteps, stopSteps, cts.Token);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.Canceled().Code, result.Error?.Code);
    }
}
