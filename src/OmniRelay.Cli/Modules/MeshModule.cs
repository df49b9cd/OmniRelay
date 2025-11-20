using System.CommandLine;

namespace OmniRelay.Cli.Modules;

/// <summary>
/// Mesh control-plane tooling commands.
/// </summary>
internal static partial class ProgramMeshModule
{
    internal static Command CreateMeshCommand()
    {
        var command = new Command("mesh", "Mesh control-plane tooling.")
        {
            Program.CreateMeshLeadersCommand(),
            Program.CreateMeshPeersCommand(),
            Program.CreateMeshUpgradeCommand(),
            Program.CreateMeshBootstrapCommand(),
            Program.CreateMeshShardsCommand(),
            Program.CreateMeshConfigCommand()
        };
        return command;
    }
}
