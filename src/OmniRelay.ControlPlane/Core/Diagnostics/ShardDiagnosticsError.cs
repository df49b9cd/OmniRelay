namespace OmniRelay.Core.Diagnostics;

/// <summary>Lightweight envelope for streaming shard diagnostics errors over SSE.</summary>
public sealed record ShardDiagnosticsError(string? Code, string? Message, IReadOnlyDictionary<string, object?>? Metadata);
