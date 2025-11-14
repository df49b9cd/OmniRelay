namespace OmniRelay.Diagnostics.Alerting;

internal sealed class NullAlertPublisher : IAlertPublisher
{
    public ValueTask PublishAsync(AlertEvent alert, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
