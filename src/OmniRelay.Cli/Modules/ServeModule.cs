#pragma warning disable IDE0005
using System.CommandLine;
using OmniRelay.Cli.Core;

namespace OmniRelay.Cli.Modules;

/// <summary>Stubbed serve module for NativeAOT builds.</summary>
internal static partial class ProgramServeModule
{
    internal static Command CreateServeCommand()
    {
        var command = new Command("serve", "Run an OmniRelay dispatcher using configuration files (disabled in NativeAOT build).");
        command.SetAction(_ =>
        {
            CliRuntime.Console.WriteError("The 'serve' command requires dynamic configuration binding and is disabled in this NativeAOT build.");
            return 1;
        });
        return command;
    }
}
