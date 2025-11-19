using OmniRelay.Configuration.Internal.TransportPolicy;

namespace OmniRelay.Cli;

internal sealed record MeshConfigValidationReport(
    string Section,
    bool HasViolations,
    bool HasExceptions,
    MeshConfigValidationFinding[] Findings)
{
    public static MeshConfigValidationReport From(string section, TransportPolicyEvaluationResult evaluation)
    {
        var findings = evaluation.Findings
            .Select(finding => new MeshConfigValidationFinding(
                finding.Endpoint,
                finding.Category,
                finding.Transport,
                finding.Encoding,
                finding.Status.ToString().ToLowerInvariant(),
                finding.Message,
                finding.ExceptionName,
                finding.ExceptionReason,
                finding.ExceptionExpiresAfter))
            .ToArray();

        return new MeshConfigValidationReport(section, evaluation.HasViolations, evaluation.HasExceptions, findings);
    }
}

internal sealed record MeshConfigValidationFinding(
    string Endpoint,
    string Category,
    string Transport,
    string Encoding,
    string Status,
    string Message,
    string? ExceptionName,
    string? ExceptionReason,
    DateTimeOffset? ExceptionExpiresAfter);
