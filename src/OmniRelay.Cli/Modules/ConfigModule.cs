#pragma warning disable IDE0005
using System.CommandLine;
using OmniRelay.Cli.Core;

namespace OmniRelay.Cli.Modules;

/// <summary>Stubbed config module for NativeAOT builds.</summary>
internal sealed class ConfigCommandsModule : ICliModule
{
    public Command Build()
    {
        var command = new Command("config", "Configuration utilities (disabled in NativeAOT build).");
        command.SetAction(_ => {
            CliRuntime.Console.WriteError("The 'config' commands rely on dynamic configuration binding and are disabled in this NativeAOT build.");
            return 1;
        });
        return command;
    }
}
