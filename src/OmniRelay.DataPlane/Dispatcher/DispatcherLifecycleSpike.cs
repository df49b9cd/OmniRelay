using System.Threading.Channels;
using Hugo;
using static Hugo.Go;

namespace OmniRelay.Dispatcher;

/// <summary>
/// Experimental helper to run start/stop steps and observe their completion order without throwing.
/// </summary>
public static class DispatcherLifecycleSpike
{
    public readonly record struct LifecycleSpikeResult(
        IReadOnlyList<string> Started,
        IReadOnlyList<string> Stopped);

    /// <summary>
    /// Runs start steps concurrently, reports their completion order, then runs stop steps sequentially.
    /// Fails fast using Hugo result pipelines instead of throwing exceptions.
    /// </summary>
    public static ValueTask<Result<LifecycleSpikeResult>> RunAsync(
        IReadOnlyList<Func<CancellationToken, ValueTask<Result<Unit>>>> startSteps,
        IReadOnlyList<Func<CancellationToken, ValueTask<Result<Unit>>>> stopSteps,
        CancellationToken cancellationToken)
    {
        if (startSteps is null)
        {
            return ValueTask.FromResult(MissingArgument(nameof(startSteps)));
        }

        if (stopSteps is null)
        {
            return ValueTask.FromResult(MissingArgument(nameof(stopSteps)));
        }

        return ExecuteAsync(startSteps, stopSteps, cancellationToken);
    }

    private static async ValueTask<Result<LifecycleSpikeResult>> ExecuteAsync(
        IReadOnlyList<Func<CancellationToken, ValueTask<Result<Unit>>>> startSteps,
        IReadOnlyList<Func<CancellationToken, ValueTask<Result<Unit>>>> stopSteps,
        CancellationToken cancellationToken)
    {
        var started = new List<string>(startSteps.Count);
        var stopped = new List<string>(stopSteps.Count);
        var readiness = MakeChannel<string>(capacity: Math.Max(1, startSteps.Count));

        using (var group = new ErrGroup(cancellationToken))
        {
            foreach (var (step, index) in startSteps.Select((step, index) => (step, index)))
            {
                group.Go((_, token) => RunStartStepAsync(step, index, readiness.Writer, token));
            }

            var waitResult = await group.WaitAsync(cancellationToken).ConfigureAwait(false);
            readiness.Writer.TryComplete();

            try
            {
                await foreach (var label in readiness.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    started.Add(label);
                }
            }
            catch (OperationCanceledException)
            {
                return Err<LifecycleSpikeResult>(Error.Canceled());
            }

            if (waitResult.IsFailure)
            {
                return waitResult.CastFailure<LifecycleSpikeResult>();
            }
        }

        foreach (var (step, index) in stopSteps.Select((step, index) => (step, index)))
        {
            var stopResult = await step(cancellationToken).ConfigureAwait(false);
            if (stopResult.IsFailure)
            {
                return stopResult.CastFailure<LifecycleSpikeResult>();
            }

            stopped.Add($"stop:{index}");
        }

        return Ok(new LifecycleSpikeResult(started, stopped));
    }

    private static async ValueTask<Result<Unit>> RunStartStepAsync(
        Func<CancellationToken, ValueTask<Result<Unit>>> step,
        int index,
        ChannelWriter<string> readinessWriter,
        CancellationToken cancellationToken)
    {
        var result = await step(cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result;
        }

        try
        {
            await readinessWriter.WriteAsync($"start:{index}", cancellationToken).ConfigureAwait(false);
            return Ok(Unit.Value);
        }
        catch (OperationCanceledException)
        {
            return Err<Unit>(Error.Canceled());
        }
        catch (Exception ex)
        {
            return Err<Unit>(Error.FromException(ex));
        }
    }

    private static Result<LifecycleSpikeResult> MissingArgument(string name) =>
        Result.Fail<LifecycleSpikeResult>(
            Error.From($"Lifecycle spike requires '{name}' to be provided.", "dispatcher.lifecycle.argument_missing")
                .WithMetadata("argument", name));
}
