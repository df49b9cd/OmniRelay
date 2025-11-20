using System.CommandLine;

namespace OmniRelay.Cli.Modules;

/// <summary>
/// Central registry for built-in CLI modules. New feature packs can be added here
/// without reflection to keep the executable NativeAOT-friendly.
/// </summary>
internal static class CliModules
{
    public static IEnumerable<ICliModule> GetDefaultModules() =>
        new ICliModule[]
        {
            new ConfigCommandsModule(),
            new RequestModule(),
            new ServeModule(),
            new IntrospectModule(),
            new ScriptModule(),
            new MeshModule()
        };
}

internal sealed class ServeModule : ICliModule { public Command Build() => Program.CreateServeCommand(); }
internal sealed class IntrospectModule : ICliModule { public Command Build() => Program.CreateIntrospectCommand(); }
internal sealed class ScriptModule : ICliModule { public Command Build() => Program.CreateScriptCommand(); }
internal sealed class MeshModule : ICliModule { public Command Build() => Program.CreateMeshCommand(); }
