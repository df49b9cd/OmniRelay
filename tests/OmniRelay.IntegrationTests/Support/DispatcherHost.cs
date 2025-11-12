using Microsoft.Extensions.Logging;
using OmniRelay.Dispatcher;

namespace OmniRelay.IntegrationTests.Support;

internal sealed class DispatcherHost : IAsyncDisposable
{
    private readonly Dispatcher.Dispatcher _dispatcher;
    private readonly ILogger _logger;

    private DispatcherHost(Dispatcher.Dispatcher dispatcher, ILogger logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public Dispatcher.Dispatcher Dispatcher => _dispatcher;

    public static Task<DispatcherHost> StartAsync(
        string name,
        DispatcherOptions options,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
        StartAsync(name, new Dispatcher.Dispatcher(options), loggerFactory, cancellationToken);

    public static async Task<DispatcherHost> StartAsync(
        string name,
        Dispatcher.Dispatcher dispatcher,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var logger = loggerFactory.CreateLogger($"DispatcherHost[{name}]");
        logger.LogInformation("Starting dispatcher for service {Service}", dispatcher.ServiceName);
        await dispatcher.StartOrThrowAsync(cancellationToken);
        logger.LogInformation("Dispatcher for service {Service} started", dispatcher.ServiceName);

        return new DispatcherHost(dispatcher, logger);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Stopping dispatcher for service {Service}", _dispatcher.ServiceName);
        await _dispatcher.StopOrThrowAsync(CancellationToken.None);
        _logger.LogInformation("Dispatcher for service {Service} stopped", _dispatcher.ServiceName);
    }
}
