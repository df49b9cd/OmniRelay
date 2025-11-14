using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OmniRelay.ControlPlane.Hosting;
using OmniRelay.ControlPlane.Security;
using Microsoft.AspNetCore.Http;
using OmniRelay.Core.Transport;
using OmniRelay.Security.Secrets;

namespace OmniRelay.ControlPlane.Bootstrap;

/// <summary>Dedicated HTTP host serving bootstrap/join endpoints.</summary>
internal sealed class BootstrapControlPlaneHost : ILifecycle, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly HttpControlPlaneHostOptions _options;
    private readonly BootstrapServerOptions _serverOptions;
    private readonly ILogger<BootstrapControlPlaneHost> _logger;
    private readonly TransportTlsManager? _tlsManager;
    private WebApplication? _app;
    private CancellationTokenSource? _cts;

    public BootstrapControlPlaneHost(
        IServiceProvider services,
        HttpControlPlaneHostOptions options,
        BootstrapServerOptions serverOptions,
        ILogger<BootstrapControlPlaneHost> logger,
        TransportTlsManager? tlsManager = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tlsManager = tlsManager;
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (_app is not null)
        {
            return;
        }

        if (_options.Urls.Count == 0)
        {
            _logger.LogWarning("Bootstrap host did not start because no URLs were configured.");
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var builder = new HttpControlPlaneHostBuilder(_options);

        var tokenService = _services.GetRequiredService<BootstrapTokenService>();
        var loggerFactory = _services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
        var secretProvider = _services.GetService<ISecretProvider>();
        var tlsManager = _tlsManager ?? new TransportTlsManager(_serverOptions.Certificate, loggerFactory.CreateLogger<TransportTlsManager>(), secretProvider);
        var bootstrapServer = new BootstrapServer(
            _serverOptions,
            tokenService,
            tlsManager,
            loggerFactory.CreateLogger<BootstrapServer>());

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(bootstrapServer);
        });

        builder.ConfigureApp(app =>
        {
            app.MapPost("/omnirelay/bootstrap/join", async (BootstrapJoinRequest request, BootstrapServer server) =>
            {
                try
                {
                    var response = server.Join(request);
                    return Results.Ok(response);
                }
                catch (BootstrapServerException ex)
                {
                    return Results.BadRequest(new { code = ex.ErrorCode, message = ex.Message });
                }
            });
        });

        var app = builder.Build();
        await app.StartAsync(_cts.Token).ConfigureAwait(false);
        _app = app;
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_app is null)
        {
            return;
        }

        using var cts = _cts;
        _cts = null;

        try
        {
            await _app.StopAsync(cancellationToken).ConfigureAwait(false);
            await _app.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _app = null;
        }
    }

    public void Dispose()
    {
        _tlsManager?.Dispose();
    }
}
