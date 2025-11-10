using Microsoft.Extensions.Options;
using OmniRelay.Dispatcher;

namespace OmniRelay.Samples.ResourceLease.MeshDemo;

public sealed class LeaseWorkerHostedService : BackgroundService
{
    private readonly ResourceLeaseHttpClient _client;
    private readonly MeshDemoOptions _options;
    private readonly ILogger<LeaseWorkerHostedService> _logger;
    private readonly Random _random = new();

    public LeaseWorkerHostedService(ResourceLeaseHttpClient client, IOptions<MeshDemoOptions> options, ILogger<LeaseWorkerHostedService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ResourceLeaseLeaseResponse? lease = null;
            try
            {
                lease = await _client.LeaseAsync(_options.WorkerPeerId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lease attempt failed; retrying.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
                continue;
            }

            if (lease is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                continue;
            }

            await ProcessLeaseAsync(lease, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessLeaseAsync(ResourceLeaseLeaseResponse lease, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing lease {ResourceType}/{ResourceId} attempt {Attempt}", lease.Payload.ResourceType, lease.Payload.ResourceId, lease.Attempt);

        try
        {
            var workDuration = TimeSpan.FromMilliseconds(_random.Next(500, 2_000));
            await Task.Delay(workDuration / 2, cancellationToken).ConfigureAwait(false);
            await _client.HeartbeatAsync(lease.OwnershipToken, cancellationToken).ConfigureAwait(false);
            await Task.Delay(workDuration / 2, cancellationToken).ConfigureAwait(false);

            if (_random.NextDouble() < 0.2)
            {
                await _client.FailAsync(lease.OwnershipToken, requeue: true, reason: "simulated failure", cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Lease {ResourceId} failed; requeued.", lease.Payload.ResourceId);
            }
            else
            {
                await _client.CompleteAsync(lease.OwnershipToken, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Lease {ResourceId} completed.", lease.Payload.ResourceId);
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker crashed processing {ResourceId}; requeueing.", lease.Payload.ResourceId);
            await _client.FailAsync(lease.OwnershipToken, requeue: true, reason: "worker exception", cancellationToken).ConfigureAwait(false);
        }
    }
}
