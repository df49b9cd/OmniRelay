using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Hugo;
using Json.Schema;
using Polymer.Errors;
using static Hugo.Go;

namespace Polymer.Core;

public sealed class JsonCodec<TRequest, TResponse> : ICodec<TRequest, TResponse>
{
    private readonly JsonSerializerOptions _options;
    private readonly JsonTypeInfo<TRequest>? _requestTypeInfo;
    private readonly JsonTypeInfo<TResponse>? _responseTypeInfo;
    private readonly JsonSchema? _requestSchema;
    private readonly string? _requestSchemaId;
    private readonly JsonSchema? _responseSchema;
    private readonly string? _responseSchemaId;
    private readonly EvaluationOptions _schemaEvaluationOptions = new() { OutputFormat = OutputFormat.List };

    public JsonCodec(
        JsonSerializerOptions? options = null,
        string encoding = "json",
        JsonSerializerContext? serializerContext = null,
        JsonSchema? requestSchema = null,
        string? requestSchemaId = null,
        JsonSchema? responseSchema = null,
        string? responseSchemaId = null)
    {
        Encoding = encoding;
        _options = options ?? serializerContext?.Options ?? CreateDefaultOptions();
        _requestTypeInfo = ResolveTypeInfo<TRequest>(serializerContext);
        _responseTypeInfo = ResolveTypeInfo<TResponse>(serializerContext);
        _requestSchema = requestSchema;
        _requestSchemaId = requestSchemaId;
        _responseSchema = responseSchema;
        _responseSchemaId = responseSchemaId;
    }

    public string Encoding { get; }

    public Result<byte[]> EncodeRequest(TRequest value, RequestMeta meta)
    {
        try
        {
            var bytes = Serialize(value, _requestTypeInfo);

            var schemaError = ValidateSchema(
                _requestSchema,
                _requestSchemaId,
                bytes,
                "encode-request",
                "request",
                meta.Procedure);

            if (schemaError is not null)
            {
                return Err<byte[]>(schemaError);
            }

            return Ok(bytes);
        }
        catch (Exception ex)
        {
            return Err<byte[]>(PolymerErrorAdapter.FromStatus(
                PolymerStatusCode.Internal,
                $"Failed to encode request for procedure '{meta.Procedure ?? "unknown"}'.",
                metadata: BuildExceptionMetadata(ex, "encode-request")));
        }
    }

    public Result<TRequest> DecodeRequest(ReadOnlyMemory<byte> payload, RequestMeta meta)
    {
        var schemaError = ValidateSchema(
            _requestSchema,
            _requestSchemaId,
            payload,
            "decode-request",
            "request",
            meta.Procedure);

        if (schemaError is not null)
        {
            return Err<TRequest>(schemaError);
        }

        try
        {
            var value = Deserialize(payload, _requestTypeInfo);
            return Ok(value!);
        }
        catch (JsonException ex)
        {
            return Err<TRequest>(PolymerErrorAdapter.FromStatus(
                PolymerStatusCode.InvalidArgument,
                $"Failed to decode request for procedure '{meta.Procedure ?? "unknown"}'.",
                metadata: BuildExceptionMetadata(ex, "decode-request")));
        }
        catch (Exception ex)
        {
            return Err<TRequest>(PolymerErrorAdapter.FromStatus(
                PolymerStatusCode.Internal,
                $"Unexpected error while decoding request for procedure '{meta.Procedure ?? "unknown"}'.",
                metadata: BuildExceptionMetadata(ex, "decode-request")));
        }
    }

    public Result<byte[]> EncodeResponse(TResponse value, ResponseMeta meta)
    {
        try
        {
            var bytes = Serialize(value, _responseTypeInfo);

            var schemaError = ValidateSchema(
                _responseSchema,
                _responseSchemaId,
                bytes,
                "encode-response",
                "response",
                meta.Transport);

            if (schemaError is not null)
            {
                return Err<byte[]>(schemaError);
            }

            return Ok(bytes);
        }
        catch (Exception ex)
        {
            return Err<byte[]>(PolymerErrorAdapter.FromStatus(
                PolymerStatusCode.Internal,
                "Failed to encode response payload.",
                metadata: BuildExceptionMetadata(ex, "encode-response")));
        }
    }

    public Result<TResponse> DecodeResponse(ReadOnlyMemory<byte> payload, ResponseMeta meta)
    {
        var schemaError = ValidateSchema(
            _responseSchema,
            _responseSchemaId,
            payload,
            "decode-response",
            "response",
            meta.Transport);

        if (schemaError is not null)
        {
            return Err<TResponse>(schemaError);
        }

        try
        {
            var value = Deserialize(payload, _responseTypeInfo);
            return Ok(value!);
        }
        catch (JsonException ex)
        {
            return Err<TResponse>(PolymerErrorAdapter.FromStatus(
                PolymerStatusCode.InvalidArgument,
                "Failed to decode response payload.",
                metadata: BuildExceptionMetadata(ex, "decode-response")));
        }
        catch (Exception ex)
        {
            return Err<TResponse>(PolymerErrorAdapter.FromStatus(
                PolymerStatusCode.Internal,
                "Unexpected error while decoding response payload.",
                metadata: BuildExceptionMetadata(ex, "decode-response")));
        }
    }

    private byte[] Serialize<T>(T value, JsonTypeInfo<T>? typeInfo) =>
        typeInfo is not null
            ? JsonSerializer.SerializeToUtf8Bytes(value, typeInfo)
            : JsonSerializer.SerializeToUtf8Bytes(value, _options);

    private T? Deserialize<T>(ReadOnlyMemory<byte> payload, JsonTypeInfo<T>? typeInfo) =>
        typeInfo is not null
            ? JsonSerializer.Deserialize(payload.Span, typeInfo)
            : JsonSerializer.Deserialize<T>(payload.Span, _options);

    private Error? ValidateSchema(
        JsonSchema? schema,
        string? schemaId,
        ReadOnlyMemory<byte> payload,
        string stage,
        string direction,
        string? identifier)
    {
        if (schema is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var evaluation = schema.Evaluate(document.RootElement, _schemaEvaluationOptions);
            if (evaluation.IsValid)
            {
                return null;
            }

            var metadata = BuildSchemaMetadata(stage, schemaId, evaluation, identifier, Encoding);
            return PolymerErrorAdapter.FromStatus(
                PolymerStatusCode.InvalidArgument,
                $"JSON schema validation failed for {direction} payload.",
                metadata: metadata);
        }
        catch (JsonException ex)
        {
            return PolymerErrorAdapter.FromStatus(
                PolymerStatusCode.InvalidArgument,
                $"Failed to parse JSON payload for schema validation ({direction}).",
                metadata: BuildExceptionMetadata(ex, stage));
        }
        catch (Exception ex)
        {
            return PolymerErrorAdapter.FromStatus(
                PolymerStatusCode.Internal,
                $"Unexpected error during JSON schema validation ({direction}).",
                metadata: BuildExceptionMetadata(ex, stage));
        }
    }

    private static JsonTypeInfo<T>? ResolveTypeInfo<T>(JsonSerializerContext? context)
    {
        if (context is null)
        {
            return null;
        }

        var typeInfo = context.GetTypeInfo(typeof(T));
        if (typeInfo is null)
        {
            if (typeof(T) == typeof(object))
            {
                return null;
            }

            throw new InvalidOperationException(
                $"JsonSerializerContext '{context.GetType().FullName}' does not expose metadata for '{typeof(T).FullName}'.");
        }

        return (JsonTypeInfo<T>)typeInfo;
    }

    private static JsonSerializerOptions CreateDefaultOptions() =>
        new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

    private static IReadOnlyDictionary<string, object?> BuildSchemaMetadata(
        string stage,
        string? schemaId,
        EvaluationResults evaluation,
        string? identifier,
        string encoding)
    {
        var errors = new List<string>();
        CollectErrors(evaluation, errors);

        return new Dictionary<string, object?>
        {
            ["encoding"] = encoding,
            ["stage"] = stage,
            ["schemaId"] = schemaId ?? "unknown",
            ["target"] = identifier ?? "unknown",
            ["errors"] = errors
        };
    }

    private static void CollectErrors(EvaluationResults results, List<string> bucket)
    {
        if (results.Errors is { Count: > 0 })
        {
            foreach (var kvp in results.Errors)
            {
                var pointer = string.IsNullOrWhiteSpace(kvp.Key) ? "#" : kvp.Key;
                bucket.Add($"{pointer}: {string.Join("; ", kvp.Value)}");
            }
        }

        if (results.Details is { Count: > 0 })
        {
            foreach (var detail in results.Details)
            {
                CollectErrors(detail, bucket);
            }
        }
    }

    private IReadOnlyDictionary<string, object?> BuildExceptionMetadata(Exception exception, string stage)
    {
        return new Dictionary<string, object?>
        {
            ["encoding"] = Encoding,
            ["stage"] = stage,
            ["exceptionType"] = exception.GetType().FullName,
            ["exceptionMessage"] = exception.Message
        };
    }
}
