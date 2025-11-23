using Hugo;

namespace OmniRelay.Dispatcher;

public static class DispatcherExtensions
{
    [Obsolete("Prefer awaiting Dispatcher.StartAsync and handling the Result instead of throwing.")]
    public static async Task StartOrThrowAsync(this Dispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        var result = await dispatcher.StartAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            throw new ResultException(result.Error!);
        }
    }

    [Obsolete("Prefer awaiting Dispatcher.StopAsync and handling the Result instead of throwing.")]
    public static async Task StopOrThrowAsync(this Dispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        var result = await dispatcher.StopAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            throw new ResultException(result.Error!);
        }
    }

    [Obsolete("Prefer Dispatcher.ClientConfig which returns Result<ClientConfiguration> instead of throwing.")]
    public static ClientConfiguration ClientConfigOrThrow(this Dispatcher dispatcher, string service)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        var result = dispatcher.ClientConfig(service);
        if (result.IsFailure)
        {
            throw new ResultException(result.Error!);
        }

        return result.Value;
    }
}
