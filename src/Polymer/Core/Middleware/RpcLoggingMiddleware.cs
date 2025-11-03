using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hugo;
using Microsoft.Extensions.Logging;
using Polymer.Core;
using Polymer.Core.Transport;
using Polymer.Errors;
using static Hugo.Go;

namespace Polymer.Core.Middleware;

public sealed class RpcLoggingMiddleware :
    IUnaryInboundMiddleware,
    IUnaryOutboundMiddleware,
    IOnewayInboundMiddleware,
    IOnewayOutboundMiddleware,
    IStreamInboundMiddleware,
    IStreamOutboundMiddleware,
    IClientStreamInboundMiddleware,
    IClientStreamOutboundMiddleware,
    IDuplexInboundMiddleware,
    IDuplexOutboundMiddleware
{
    private readonly ILogger _logger;
    private readonly RpcLoggingOptions _options;

    public RpcLoggingMiddleware(ILogger<RpcLoggingMiddleware> logger, RpcLoggingOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new RpcLoggingOptions();
    }

    public ValueTask<Result<Response<ReadOnlyMemory<byte>>>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        UnaryInboundDelegate next) =>
        ExecuteWithLogging(
            "inbound unary",
            request.Meta,
            token => next(request, token),
            cancellationToken,
            static response => response.Meta);

    public ValueTask<Result<Response<ReadOnlyMemory<byte>>>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        UnaryOutboundDelegate next) =>
        ExecuteWithLogging(
            "outbound unary",
            request.Meta,
            token => next(request, token),
            cancellationToken,
            static response => response.Meta);

    public ValueTask<Result<OnewayAck>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        OnewayInboundDelegate next) =>
        ExecuteWithLogging(
            "inbound oneway",
            request.Meta,
            token => next(request, token),
            cancellationToken,
            static ack => ack.Meta);

    public ValueTask<Result<OnewayAck>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        OnewayOutboundDelegate next) =>
        ExecuteWithLogging(
            "outbound oneway",
            request.Meta,
            token => next(request, token),
            cancellationToken,
            static ack => ack.Meta);

    public ValueTask<Result<IStreamCall>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        StreamCallOptions options,
        CancellationToken cancellationToken,
        StreamInboundDelegate next) =>
        ExecuteWithLogging(
            $"inbound stream ({options.Direction})",
            request.Meta,
            token => next(request, options, token),
            cancellationToken);

    public ValueTask<Result<IStreamCall>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        StreamCallOptions options,
        CancellationToken cancellationToken,
        StreamOutboundDelegate next) =>
        ExecuteWithLogging(
            $"outbound stream ({options.Direction})",
            request.Meta,
            token => next(request, options, token),
            cancellationToken);

    public ValueTask<Result<Response<ReadOnlyMemory<byte>>>> InvokeAsync(
        ClientStreamRequestContext context,
        CancellationToken cancellationToken,
        ClientStreamInboundDelegate next) =>
        ExecuteWithLogging(
            "inbound client-stream",
            context.Meta,
            token => next(context, token),
            cancellationToken,
            static response => response.Meta);

    public ValueTask<Result<IClientStreamTransportCall>> InvokeAsync(
        RequestMeta requestMeta,
        CancellationToken cancellationToken,
        ClientStreamOutboundDelegate next) =>
        ExecuteWithLogging(
            "outbound client-stream",
            requestMeta,
            token => next(requestMeta, token),
            cancellationToken);

    public ValueTask<Result<IDuplexStreamCall>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        DuplexInboundDelegate next) =>
        ExecuteWithLogging(
            "inbound duplex",
            request.Meta,
            token => next(request, token),
            cancellationToken);

    public ValueTask<Result<IDuplexStreamCall>> InvokeAsync(
        IRequest<ReadOnlyMemory<byte>> request,
        CancellationToken cancellationToken,
        DuplexOutboundDelegate next) =>
        ExecuteWithLogging(
            "outbound duplex",
            request.Meta,
            token => next(request, token),
            cancellationToken);

    private async ValueTask<Result<TResponse>> ExecuteWithLogging<TResponse>(
        string pipeline,
        RequestMeta meta,
        Func<CancellationToken, ValueTask<Result<TResponse>>> invoke,
        CancellationToken cancellationToken,
        Func<TResponse, ResponseMeta>? responseMetaAccessor = null)
    {
        var shouldLog = ShouldLog(meta);
        var start = Stopwatch.GetTimestamp();

        try
        {
            var result = await invoke(cancellationToken).ConfigureAwait(false);
            var duration = Stopwatch.GetElapsedTime(start);

            if (result.IsSuccess)
            {
                if (shouldLog)
                {
                    var responseMeta = responseMetaAccessor is null ? null : responseMetaAccessor(result.Value);
                    LogSuccess(pipeline, meta, duration, responseMeta);
                }
            }
            else if (ShouldLogFailure(result.Error!, shouldLog))
            {
                LogFailure(pipeline, meta, duration, result.Error!);
            }

            return result;
        }
        catch (Exception ex)
        {
            if (!shouldLog && !_logger.IsEnabled(_options.FailureLogLevel))
            {
                throw;
            }

            var duration = Stopwatch.GetElapsedTime(start);
            LogException(pipeline, meta, duration, ex);
            throw;
        }
    }

    private bool ShouldLog(RequestMeta meta) =>
        _options.ShouldLogRequest?.Invoke(meta) ?? true;

    private bool ShouldLogFailure(Error error, bool loggedRequest) =>
        _options.ShouldLogError?.Invoke(error) ?? loggedRequest;

    private void LogSuccess(string pipeline, RequestMeta meta, TimeSpan duration, ResponseMeta? responseMeta)
    {
        if (!_logger.IsEnabled(_options.SuccessLogLevel))
        {
            return;
        }

        _logger.Log(
            _options.SuccessLogLevel,
            "rpc {Pipeline} completed in {DurationMs:F2} ms (service={Service} procedure={Procedure} transport={Transport} caller={Caller} encoding={Encoding} responseEncoding={ResponseEncoding})",
            pipeline,
            duration.TotalMilliseconds,
            meta.Service,
            meta.Procedure ?? string.Empty,
            meta.Transport ?? "unknown",
            meta.Caller ?? string.Empty,
            meta.Encoding ?? "unknown",
            responseMeta?.Encoding ?? "unknown");
    }

    private void LogFailure(string pipeline, RequestMeta meta, TimeSpan duration, Error error)
    {
        if (!_logger.IsEnabled(_options.FailureLogLevel))
        {
            return;
        }

        var status = PolymerErrorAdapter.ToStatus(error);

        _logger.Log(
            _options.FailureLogLevel,
            "rpc {Pipeline} failed in {DurationMs:F2} ms (service={Service} procedure={Procedure} transport={Transport} status={Status} code={Code}) - {ErrorMessage}",
            pipeline,
            duration.TotalMilliseconds,
            meta.Service,
            meta.Procedure ?? string.Empty,
            meta.Transport ?? "unknown",
            status,
            error.Code ?? string.Empty,
            error.Message ?? "unknown failure");
    }

    private void LogException(string pipeline, RequestMeta meta, TimeSpan duration, Exception exception)
    {
        if (!_logger.IsEnabled(_options.FailureLogLevel))
        {
            return;
        }

        _logger.Log(
            _options.FailureLogLevel,
            exception,
            "rpc {Pipeline} threw in {DurationMs:F2} ms (service={Service} procedure={Procedure} transport={Transport})",
            pipeline,
            duration.TotalMilliseconds,
            meta.Service,
            meta.Procedure ?? string.Empty,
            meta.Transport ?? "unknown");
    }
}
