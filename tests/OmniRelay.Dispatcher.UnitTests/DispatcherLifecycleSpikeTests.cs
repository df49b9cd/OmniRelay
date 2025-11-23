using Hugo;
using Xunit;
using static Hugo.Go;

namespace OmniRelay.Dispatcher.UnitTests;

public class DispatcherLifecycleSpikeTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public async ValueTask RunAsync_ReportsStartAndStopOrder()
    {
        var startSteps = new List<Func<CancellationToken, ValueTask<Result<Unit>>>>
        {
            async token =>
            {
                await Task.Delay(10, token);
                return Ok(Unit.Value);
            },
            async token =>
            {
                await Task.Delay(1, token);
                return Ok(Unit.Value);
            }
        };

        var stopSteps = new List<Func<CancellationToken, ValueTask<Result<Unit>>>>
        {
            token => new ValueTask<Result<Unit>>(Ok(Unit.Value)),
            token => new ValueTask<Result<Unit>>(Ok(Unit.Value))
        };

        var result = await DispatcherLifecycleSpike.RunAsync(startSteps, stopSteps, CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Message);

        var (started, stopped) = result.Value;
        Assert.Contains("start:0", started);
        Assert.Contains("start:1", started);
        Assert.Equal(["stop:0", "stop:1"], stopped);
    }
}
