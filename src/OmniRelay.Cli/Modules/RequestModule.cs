using System.CommandLine;
using OmniRelay.Cli.Core;

namespace OmniRelay.Cli.Modules;

/// <summary>
/// Request and benchmark commands.
/// </summary>
internal sealed class RequestModule : ICliModule
{
    public Command Build()
    {
        var command = new Command("request", "Invoke a unary RPC over HTTP or gRPC.");
        CommandBuilder.AttachRequest(command);

        var benchmark = new Command("benchmark", "Run concurrent unary RPC load tests over HTTP or gRPC.");
        CommandBuilder.AttachBenchmark(benchmark);

        var root = new Command("rpc", "RPC invocation and load testing")
        {
            command,
            benchmark
        };
        return root;
    }
}

// Thin adapter that reuses the existing builders in Program for now.
internal static class CommandBuilder
{
    public static void AttachRequest(Command command)
    {
        command.AddAlias("call");
        Program.ConfigureRequestCommand(command);
    }

    public static void AttachBenchmark(Command command)
    {
        command.AddAlias("bench");
        Program.ConfigureBenchmarkCommand(command);
    }
}
