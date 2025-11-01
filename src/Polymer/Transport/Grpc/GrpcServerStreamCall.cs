using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hugo;
using Polymer.Core;
using Polymer.Core.Transport;
using Polymer.Errors;
using static Hugo.Go;

namespace Polymer.Transport.Grpc;

public sealed class GrpcServerStreamCall : IStreamCall
{
    private readonly Channel<ReadOnlyMemory<byte>> _responses;
    private readonly Channel<ReadOnlyMemory<byte>> _requests;
    private bool _completed;

    private GrpcServerStreamCall(RequestMeta requestMeta, ResponseMeta responseMeta)
    {
        RequestMeta = requestMeta ?? throw new ArgumentNullException(nameof(requestMeta));
        ResponseMeta = responseMeta ?? new ResponseMeta();

        _responses = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _requests = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
        _requests.Writer.TryComplete();
    }

    public static GrpcServerStreamCall Create(RequestMeta requestMeta, ResponseMeta? responseMeta = null) =>
        new(requestMeta, responseMeta ?? new ResponseMeta());

    public StreamDirection Direction => StreamDirection.Server;

    public RequestMeta RequestMeta { get; }

    public ResponseMeta ResponseMeta { get; private set; }

    public ChannelWriter<ReadOnlyMemory<byte>> Requests => _requests.Writer;

    public ChannelReader<ReadOnlyMemory<byte>> Responses => _responses.Reader;

    public void SetResponseMeta(ResponseMeta meta)
    {
        ResponseMeta = meta ?? new ResponseMeta();
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default) =>
        _responses.Writer.WriteAsync(payload, cancellationToken);

    public ValueTask CompleteAsync(Error? error = null, CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return ValueTask.CompletedTask;
        }

        _completed = true;

        if (error is null)
        {
            _responses.Writer.TryComplete();
        }
        else
        {
            var exception = PolymerErrors.FromError(error, GrpcTransportConstants.TransportName);
            _responses.Writer.TryComplete(exception);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _responses.Writer.TryComplete();
        _requests.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
