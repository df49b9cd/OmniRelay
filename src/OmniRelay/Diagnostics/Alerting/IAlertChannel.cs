namespace OmniRelay.Diagnostics.Alerting;

internal interface IAlertChannel
{
    string Name { get; }

    ValueTask SendAsync(AlertEvent alert, CancellationToken cancellationToken);
}
