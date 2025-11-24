using Hugo;
using static Hugo.Go;

namespace OmniRelay.ControlPlane.Events;

/// <summary>Provides a process-local event bus for control-plane lifecycle updates.</summary>
public interface IControlPlaneEventBus
{
    ValueTask<Result<ControlPlaneEventSubscription>> SubscribeAsync(
        ControlPlaneEventFilter? filter = null,
        int capacity = 256,
        CancellationToken cancellationToken = default);

    ValueTask<Result<Unit>> PublishAsync(ControlPlaneEvent message, CancellationToken cancellationToken = default);
}
