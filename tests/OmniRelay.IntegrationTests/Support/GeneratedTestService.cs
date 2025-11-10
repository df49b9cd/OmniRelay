using System.Threading;
using System.Threading.Tasks;
using OmniRelay.Core;
using OmniRelay.Core.Transport;
using OmniRelay.Dispatcher;
using OmniRelay.Tests.Protos;
using OmniRelay.Transport.Grpc;

namespace OmniRelay.IntegrationTests.Support;

internal sealed class GeneratedTestService : TestServiceOmniRelay.ITestService
{
    public TaskCompletionSource<RequestMeta> UnaryMeta
    {
        get => field;
    } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource<RequestMeta> ServerStreamMeta
    {
        get => field;
    } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource<RequestMeta> ClientStreamMeta
    {
        get => field;
    } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource<RequestMeta> DuplexMeta
    {
        get => field;
    } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValueTask<Response<UnaryResponse>> UnaryCallAsync(Request<UnaryRequest> request, CancellationToken cancellationToken)
    {
        UnaryMeta.TrySetResult(request.Meta);
        var payload = new UnaryResponse { Message = $"{request.Body.Message}-unary-response" };
        return ValueTask.FromResult(Response<UnaryResponse>.Create(payload, new ResponseMeta(encoding: "protobuf")));
    }

    public async ValueTask ServerStreamAsync(Request<StreamRequest> request, ProtobufCallAdapters.ProtobufServerStreamWriter<StreamRequest, StreamResponse> stream, CancellationToken cancellationToken)
    {
        ServerStreamMeta.TrySetResult(request.Meta);
        for (var index = 0; index < 3; index++)
        {
            var writeResult = await stream.WriteAsync(new StreamResponse { Value = $"{request.Body.Value}#{index}" }, cancellationToken).ConfigureAwait(false);
            writeResult.ThrowIfFailure();
        }
    }

    public async ValueTask<Response<UnaryResponse>> ClientStreamAsync(ProtobufCallAdapters.ProtobufClientStreamContext<StreamRequest, UnaryResponse> context, CancellationToken cancellationToken)
    {
        ClientStreamMeta.TrySetResult(context.Meta);
        var sum = 0;
        await foreach (var chunkResult in context.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            var chunk = chunkResult.ValueOrThrow();
            _ = int.TryParse(chunk.Value, out var value);
            sum += value;
        }

        var payload = new UnaryResponse { Message = $"sum:{sum}" };
        return Response<UnaryResponse>.Create(payload, new ResponseMeta(encoding: "protobuf"));
    }

    public async ValueTask DuplexStreamAsync(ProtobufCallAdapters.ProtobufDuplexStreamContext<StreamRequest, StreamResponse> context, CancellationToken cancellationToken)
    {
        DuplexMeta.TrySetResult(context.RequestMeta);
        var initialWrite = await context.WriteAsync(new StreamResponse { Value = "ready" }, cancellationToken).ConfigureAwait(false);
        initialWrite.ThrowIfFailure();

        await foreach (var chunkResult in context.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            var chunk = chunkResult.ValueOrThrow();
            var writeResult = await context.WriteAsync(new StreamResponse { Value = $"echo:{chunk.Value}" }, cancellationToken).ConfigureAwait(false);
            writeResult.ThrowIfFailure();
        }
    }

}
