using Google.Protobuf.Reflection;
using Microsoft.CodeAnalysis;
using Polymer.Codegen.Protobuf.Core;

namespace Polymer.Codegen.Protobuf.Generator;

[Generator]
public sealed class ProtobufIncrementalGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor DescriptorReadError = new(
        id: "POLYPROT001",
        title: "Failed to read descriptor set",
        messageFormat: "Unable to read protobuf descriptor '{0}': {1}",
        category: "Polymer.Codegen",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DescriptorParseError = new(
        id: "POLYPROT002",
        title: "Failed to parse descriptor set",
        messageFormat: "Unable to parse protobuf descriptor '{0}': {1}",
        category: "Polymer.Codegen",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var descriptorSets = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".pb", StringComparison.OrdinalIgnoreCase))
            .Select((file, cancellationToken) => ReadDescriptorSet(file.Path))
            .Where(static result => result is not null);

        context.RegisterSourceOutput(descriptorSets, (spc, result) =>
        {
            if (result is null)
            {
                return;
            }

            if (result.ReadException is not null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(DescriptorReadError, Location.None, result.Path, result.ReadException.Message));
                return;
            }

            if (result.ParseException is not null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(DescriptorParseError, Location.None, result.Path, result.ParseException.Message));
                return;
            }

            if (result.DescriptorSet is null)
            {
                return;
            }

            var generator = new PolymerProtobufGenerator();
            foreach (var file in generator.GenerateFiles(result.DescriptorSet))
            {
                var hintName = CreateHintName(result.Path, file.Name);
                spc.AddSource(hintName, file.Content);
            }
        });
    }

    private static DescriptorResult? ReadDescriptorSet(string path)
    {
#pragma warning disable RS1035 // Do not do file IO in analyzers
        try
        {
            var bytes = File.ReadAllBytes(path);
            try
            {
                var descriptorSet = FileDescriptorSet.Parser.ParseFrom(bytes);
                return new DescriptorResult(path, descriptorSet, null, null);
            }
            catch (Exception parseEx)
            {
                return new DescriptorResult(path, null, null, parseEx);
            }
        }
        catch (Exception readEx)
        {
            return new DescriptorResult(path, null, readEx, null);
        }
#pragma warning restore RS1035
    }

    private static string CreateHintName(string descriptorPath, string generatedFileName)
    {
        var descriptorName = Path.GetFileNameWithoutExtension(descriptorPath);
        var sanitized = generatedFileName
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace('.', '_');
        return $"{descriptorName}_{sanitized}.g.cs";
    }

    private sealed record DescriptorResult(
        string Path,
        FileDescriptorSet? DescriptorSet,
        Exception? ReadException,
        Exception? ParseException);
}
