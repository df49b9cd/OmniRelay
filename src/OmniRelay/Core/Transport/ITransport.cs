namespace OmniRelay.Core.Transport;

public interface ITransport : ILifecycle
{
    string Name { get; }
}
