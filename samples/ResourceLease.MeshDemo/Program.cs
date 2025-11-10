using System.Text.Json;
using Microsoft.Extensions.Options;
using OmniRelay.Core.Peers;
using OmniRelay.Dispatcher;
using OmniRelay.Samples.ResourceLease.MeshDemo;
using OmniRelayDispatcher = OmniRelay.Dispatcher.Dispatcher;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables(prefix: "MESHDEMO_");

builder.Services.Configure<MeshDemoOptions>(builder.Configuration.GetSection("meshDemo"));

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<MeshDemoOptions>>().Value;
    return MeshDemoPaths.Create(options);
});

builder.Services.AddSingleton<PeerLeaseHealthTracker>();
builder.Services.AddSingleton<BackpressureAwareRateLimiter>(sp =>
{
    var limiter = new BackpressureAwareRateLimiter(
        normalLimiter: BackpressureLimiterFactory.Create(permitLimit: 32),
        backpressureLimiter: BackpressureLimiterFactory.Create(permitLimit: 4));
    return limiter;
});

builder.Services.AddSingleton<ResourceLeaseBackpressureDiagnosticsListener>();
builder.Services.AddSingleton<IResourceLeaseBackpressureListener>(sp =>
    sp.GetRequiredService<ResourceLeaseBackpressureDiagnosticsListener>());
builder.Services.AddSingleton<IResourceLeaseBackpressureListener>(sp =>
    new RateLimitingBackpressureListener(
        sp.GetRequiredService<BackpressureAwareRateLimiter>(),
        sp.GetRequiredService<ILogger<RateLimitingBackpressureListener>>()));

builder.Services.AddSingleton<MeshReplicationLog>();
builder.Services.AddSingleton<IResourceLeaseReplicationSink>(sp =>
    new MeshReplicationLogSink(sp.GetRequiredService<MeshReplicationLog>()));

builder.Services.AddSingleton(sp =>
{
    var paths = sp.GetRequiredService<MeshDemoPaths>();
    var sinks = sp.GetServices<IResourceLeaseReplicationSink>();
    return new SqliteResourceLeaseReplicator(paths.ReplicationConnectionString, tableName: "LeaseEvents", sinks: sinks);
});

builder.Services.AddSingleton(sp =>
{
    var paths = sp.GetRequiredService<MeshDemoPaths>();
    return new SqliteDeterministicStateStore(paths.DeterministicConnectionString);
});

builder.Services.AddSingleton<MeshDispatcherHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MeshDispatcherHostedService>());

builder.Services.AddHttpClient<ResourceLeaseHttpClient>((sp, httpClient) =>
{
    var options = sp.GetRequiredService<IOptions<MeshDemoOptions>>().Value;
    httpClient.BaseAddress = options.GetRpcBaseUri();
    httpClient.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddHostedService<LeaseSeederHostedService>();
builder.Services.AddHostedService<LeaseWorkerHostedService>();

var app = builder.Build();

app.MapGet("/", () => Results.Text("""
OmniRelay ResourceLease Mesh Demo

Key endpoints:
- POST /demo/enqueue            -> enqueue sample work items without CLI
- GET  /demo/lease-health       -> PeerLeaseHealthTracker snapshot (JSON)
- GET  /demo/backpressure       -> Last backpressure signal (JSON)
- GET  /demo/replication        -> Recent replication events (JSON)

RPC endpoints:
- ResourceLease dispatcher listens on http://127.0.0.1:7420/yarpc/v1 (namespace resourcelease.mesh)

Try commands:
  omnirelay request --transport http --url http://127.0.0.1:7420/yarpc/v1 \
    --service resourcelease-mesh-demo \
    --procedure resourcelease.mesh::enqueue \
    --encoding application/json \
    --body '{"payload":{"resourceType":"demo","resourceId":"cli","partitionKey":"cli","payloadEncoding":"json","body":"eyJtZXNzYWdlIjoiY2xpIn0="}}'
"""));

app.MapPost("/demo/enqueue", async (MeshEnqueueRequest request, ResourceLeaseHttpClient client, CancellationToken ct) =>
{
    var payload = request.ToPayload();
    var response = await client.EnqueueAsync(payload, ct);
    return Results.Json(response, MeshJson.Options);
});

app.MapGet("/demo/lease-health", (PeerLeaseHealthTracker tracker) =>
{
    var snapshots = tracker.Snapshot();
    return Results.Json(PeerLeaseHealthDiagnostics.FromSnapshots(snapshots), MeshJson.Options);
});

app.MapGet("/demo/backpressure", (ResourceLeaseBackpressureDiagnosticsListener listener) =>
{
    return listener.Latest is { } latest
        ? Results.Json(latest, MeshJson.Options)
        : Results.NoContent();
});

app.MapGet("/demo/replication", (MeshReplicationLog log) =>
{
    return Results.Json(log.GetRecent(), MeshJson.Options);
});

app.Run();
