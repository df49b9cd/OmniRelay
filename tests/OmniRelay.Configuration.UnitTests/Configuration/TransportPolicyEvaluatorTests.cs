using OmniRelay.Configuration.Internal.TransportPolicy;
using OmniRelay.Configuration.Models;
using Shouldly;
using Xunit;

namespace OmniRelay.Configuration.UnitTests.Configuration;

public sealed class TransportPolicyEvaluatorTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public void Enforce_WithHttpDiagnosticsDowngrade_PassesWhenAllowed()
    {
        var options = CreateBaseOptions();
        options.Diagnostics.ControlPlane.HttpRuntime.EnableHttp3 = false;

        Should.NotThrow(() => TransportPolicyEvaluator.Enforce(options));
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void Enforce_WithException_AllowsDowngrade_ButIsCompliantByDefault()
    {
        var options = CreateBaseOptions();
        options.Diagnostics.ControlPlane.HttpRuntime.EnableHttp3 = false;
        options.TransportPolicy.Exceptions.Add(new TransportPolicyExceptionConfiguration
        {
            Name = "legacy-json",
            Category = TransportPolicyCategories.Diagnostics,
            AppliesTo = { TransportPolicyEndpoints.DiagnosticsHttp },
            Transports = { TransportPolicyTransports.Http2 },
            Encodings = { TransportPolicyEncodings.Json },
            Reason = "Legacy observability stack",
            ExpiresAfter = DateTimeOffset.UtcNow.AddDays(30)
        });

        var result = TransportPolicyEvaluator.Evaluate(options);
        result.HasViolations.ShouldBeFalse();
        result.HasExceptions.ShouldBeFalse();
        result.Findings.ShouldAllBe(finding => finding.Status == TransportPolicyFindingStatus.Compliant);
        Should.NotThrow(() => TransportPolicyEvaluator.Enforce(options));
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void Evaluate_WithDowngrade_ComputesSummaryAndHints()
    {
        var options = CreateBaseOptions();
        options.Diagnostics.ControlPlane.HttpRuntime.EnableHttp3 = false;

        var evaluation = TransportPolicyEvaluator.Evaluate(options);

        evaluation.Summary.Total.ShouldBe(2);
        evaluation.Summary.Violations.ShouldBe(0);
        evaluation.Summary.Compliant.ShouldBe(2);
        evaluation.Summary.Excepted.ShouldBe(0);

        var httpFinding = evaluation.Findings.First(finding => finding.Endpoint == TransportPolicyEndpoints.DiagnosticsHttp);
        httpFinding.Http3Enabled.ShouldBeFalse();
        var hint = httpFinding.Hint;
        hint.ShouldNotBeNull();
        hint!.ShouldContain("enableHttp3", Case.Insensitive);
    }

    private static OmniRelayConfigurationOptions CreateBaseOptions()
    {
        var options = new OmniRelayConfigurationOptions
        {
            Service = "policy-tests",
        };

        options.Diagnostics.ControlPlane.HttpUrls.Clear();
        options.Diagnostics.ControlPlane.HttpUrls.Add("https://127.0.0.1:9095");
        options.Diagnostics.ControlPlane.HttpRuntime.EnableHttp3 = true;

        options.Diagnostics.ControlPlane.GrpcUrls.Clear();
        options.Diagnostics.ControlPlane.GrpcUrls.Add("https://127.0.0.1:9096");
        options.Diagnostics.ControlPlane.GrpcRuntime.EnableHttp3 = true;

        return options;
    }
}
