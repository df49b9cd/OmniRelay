using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Hugo;

namespace YarpcDotNet.Core.Errors;

public static class YarpcErrorAdapter
{
    private const string StatusMetadataKey = "yarpc.status";
    private const string TransportMetadataKey = "yarpc.transport";
    private static readonly ImmutableDictionary<YarpcStatusCode, string> StatusCodeNames = new[]
    {
        (YarpcStatusCode.Unknown, "unknown"),
        (YarpcStatusCode.Cancelled, "cancelled"),
        (YarpcStatusCode.InvalidArgument, "invalid-argument"),
        (YarpcStatusCode.DeadlineExceeded, "deadline-exceeded"),
        (YarpcStatusCode.NotFound, "not-found"),
        (YarpcStatusCode.AlreadyExists, "already-exists"),
        (YarpcStatusCode.PermissionDenied, "permission-denied"),
        (YarpcStatusCode.ResourceExhausted, "resource-exhausted"),
        (YarpcStatusCode.FailedPrecondition, "failed-precondition"),
        (YarpcStatusCode.Aborted, "aborted"),
        (YarpcStatusCode.OutOfRange, "out-of-range"),
        (YarpcStatusCode.Unimplemented, "unimplemented"),
        (YarpcStatusCode.Internal, "internal"),
        (YarpcStatusCode.Unavailable, "unavailable"),
        (YarpcStatusCode.DataLoss, "data-loss")
    }.ToImmutableDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

    public static Error FromStatus(
        YarpcStatusCode code,
        string message,
        string? transport = null,
        Error? inner = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        var error = inner ?? Error.From(message, StatusCodeNames[code]);
        error = error.WithCode(StatusCodeNames[code]).WithMetadata(StatusMetadataKey, code.ToString());

        if (!string.IsNullOrEmpty(transport))
        {
            error = error.WithMetadata(TransportMetadataKey, transport);
        }

        if (metadata is { Count: > 0 })
        {
            foreach (var kvp in metadata)
            {
                error = error.WithMetadata(kvp.Key, kvp.Value);
            }
        }

        return error;
    }

    public static YarpcStatusCode ToStatus(Error error)
    {
        if (error.TryGetMetadata(StatusMetadataKey, out string? value) &&
            Enum.TryParse<YarpcStatusCode>(value, out var parsed))
        {
            return parsed;
        }

        if (!string.IsNullOrEmpty(error.Code))
        {
            foreach (var (status, codeName) in StatusCodeNames)
            {
                if (string.Equals(error.Code, codeName, StringComparison.OrdinalIgnoreCase))
                {
                    return status;
                }
            }
        }

        if (error.Cause is OperationCanceledException)
        {
            return YarpcStatusCode.Cancelled;
        }

        return YarpcStatusCode.Unknown;
    }

    public static Error WithStatusMetadata(Error error, YarpcStatusCode code) =>
        error.WithCode(StatusCodeNames[code]).WithMetadata(StatusMetadataKey, code.ToString());
}
