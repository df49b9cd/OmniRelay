using AwesomeAssertions;
using OmniRelay.Core;
using OmniRelay.Core.Middleware;
using Xunit;
using static AwesomeAssertions.FluentActions;
using static Hugo.Go;

namespace OmniRelay.Dispatcher.UnitTests;

public class ProcedureSpecTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public void FullName_ComposesServiceAndName()
    {
        var spec = new UnaryProcedureSpec(
            "svc",
            "proc",
            (_, _) => ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty))));

        spec.FullName.Should().Be("svc::proc");
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void Constructor_WithWhitespaceAlias_Throws()
    {
        var middleware = Array.Empty<IUnaryInboundMiddleware>();

        Invoking(() =>
                new UnaryProcedureSpec(
                    "svc",
                    "proc",
                    (_, _) => ValueTask.FromResult(Ok(Response<ReadOnlyMemory<byte>>.Create(ReadOnlyMemory<byte>.Empty))),
                    aliases: ["valid", "  "]))
            .Should().Throw<ArgumentException>();
    }
}
