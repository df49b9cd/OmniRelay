using System.Text.Json;
using Microsoft.Extensions.Options;
using OmniRelay.Dispatcher;

namespace OmniRelay.Samples.ResourceLease.MeshDemo;

public sealed class LeaseSeederHostedService : BackgroundService
{
    private readonly ResourceLeaseHttpClient _client;
    private readonly MeshDemoOptions _options;
    private readonly ILogger<LeaseSeederHostedService> _logger;
    private int _sequence;

    public LeaseSeederHostedService(ResourceLeaseHttpClient client, IOptions<MeshDemoOptions> options, ILogger<LeaseSeederHostedService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = _options.GetSeederInterval();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var id = Interlocked.Increment(ref _sequence);
                var payload = new ResourceLeaseItemPayload(
                    ResourceType: "demo.order",
                    ResourceId: $"order-{id:D4}",
                    PartitionKey: $"tenant-{(id % 3) + 1}",
                    PayloadEncoding: "application/json",
                    Body: MeshJsonPayload(id),
                    Attributes: new Dictionary<string, string>
                    {
                        ["tenant"] = $"tenant-{(id % 3) + 1}",
                        ["priority"] = id % 5 == 0 ? "high" : "normal"
                    },
                    RequestId: Guid.NewGuid().ToString("N"));

                var response = await _client.EnqueueAsync(payload, stoppingToken).ConfigureAwait(false);
                _logger.LogInformation("Enqueued {ResourceId} (pending={Pending}, active={Active})", payload.ResourceId, response.Stats.PendingCount, response.Stats.ActiveLeaseCount);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enqueue demo work item.");
            }

            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static byte[] MeshJsonPayload(int id) =>
        JsonSerializer.SerializeToUtf8Bytes(
            new LeaseSeederPayload(id, (id % 5 + 1) * 10, DateTimeOffset.UtcNow),
            MeshJson.Context.LeaseSeederPayload);
}

internal sealed record LeaseSeederPayload(int OrderId, int Amount, DateTimeOffset CreatedAt);
