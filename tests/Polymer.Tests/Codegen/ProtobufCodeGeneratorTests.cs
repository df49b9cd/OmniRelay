using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Compiler;
using Google.Protobuf.Reflection;
using Xunit;

namespace Polymer.Tests.Codegen;

public class ProtobufCodeGeneratorTests
{
    [Fact]
    public void Generated_Code_Matches_Golden_File()
    {
        var request = CodeGeneratorRequestFactory.Create();
        var response = CodeGeneratorProcessRunner.Execute(request);

        Assert.Single(response.File);
        var generated = response.File[0].Content.Replace("\r\n", "\n");
        var goldenPath = TestPath.Combine("tests", "Polymer.Tests", "Generated", "TestService.Polymer.g.cs");
        var expected = File.ReadAllText(goldenPath).Replace("\r\n", "\n");

        Assert.Equal(expected, generated);
    }

    private static class CodeGeneratorRequestFactory
    {
        public static CodeGeneratorRequest Create()
        {
            var file = new FileDescriptorProto
            {
                Name = "tests/Polymer.Tests/Protos/test_service.proto",
                Package = "polymer.tests.codegen",
                Options = new Google.Protobuf.Reflection.FileOptions { CsharpNamespace = "Polymer.Tests.Protos" }
            };

            file.MessageType.Add(CreateMessage("UnaryRequest", ("message", FieldDescriptorProto.Types.Type.String)));
            file.MessageType.Add(CreateMessage("UnaryResponse", ("message", FieldDescriptorProto.Types.Type.String)));
            file.MessageType.Add(CreateMessage("StreamRequest", ("value", FieldDescriptorProto.Types.Type.String)));
            file.MessageType.Add(CreateMessage("StreamResponse", ("value", FieldDescriptorProto.Types.Type.String)));

            var service = new ServiceDescriptorProto { Name = "TestService" };
            service.Method.Add(new MethodDescriptorProto
            {
                Name = "UnaryCall",
                InputType = ".polymer.tests.codegen.UnaryRequest",
                OutputType = ".polymer.tests.codegen.UnaryResponse"
            });
            service.Method.Add(new MethodDescriptorProto
            {
                Name = "ServerStream",
                InputType = ".polymer.tests.codegen.StreamRequest",
                OutputType = ".polymer.tests.codegen.StreamResponse",
                ServerStreaming = true
            });
            service.Method.Add(new MethodDescriptorProto
            {
                Name = "ClientStream",
                InputType = ".polymer.tests.codegen.StreamRequest",
                OutputType = ".polymer.tests.codegen.UnaryResponse",
                ClientStreaming = true
            });
            service.Method.Add(new MethodDescriptorProto
            {
                Name = "DuplexStream",
                InputType = ".polymer.tests.codegen.StreamRequest",
                OutputType = ".polymer.tests.codegen.StreamResponse",
                ClientStreaming = true,
                ServerStreaming = true
            });

            file.Service.Add(service);

            var request = new CodeGeneratorRequest();
            request.FileToGenerate.Add(file.Name);
            request.ProtoFile.Add(file);
            return request;
        }

        private static DescriptorProto CreateMessage(string name, params (string Name, FieldDescriptorProto.Types.Type Type)[] fields)
        {
            var descriptor = new DescriptorProto { Name = name };
            uint number = 1;
            foreach (var field in fields)
            {
                descriptor.Field.Add(new FieldDescriptorProto
                {
                    Name = field.Name,
                    Number = (int)number++,
                    Label = FieldDescriptorProto.Types.Label.Optional,
                    Type = field.Type
                });
            }

            return descriptor;
        }
    }

    private static class CodeGeneratorProcessRunner
    {
        public static CodeGeneratorResponse Execute(CodeGeneratorRequest request)
        {
            var pluginPath = LocatePluginAssembly();
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{pluginPath}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start protoc-gen-polymer-csharp.");

            using (var codedOutput = new CodedOutputStream(process.StandardInput.BaseStream, leaveOpen: true))
            {
                request.WriteTo(codedOutput);
                codedOutput.Flush();
            }

            process.StandardInput.Close();
            var response = CodeGeneratorResponse.Parser.ParseFrom(process.StandardOutput.BaseStream);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Code generator exited with code {process.ExitCode}. Error: {response.Error}");
            }

            return response;
        }

        private static string LocatePluginAssembly()
        {
            var solutionRoot = TestPath.Root;
            var pluginDirectory = Path.Combine(solutionRoot, "src", "Polymer.Codegen.Protobuf", "bin");
            if (!Directory.Exists(pluginDirectory))
            {
                throw new DirectoryNotFoundException($"Plug-in build directory not found: {pluginDirectory}");
            }

            var candidates = new[]
            {
                Path.Combine(pluginDirectory, "Debug", "net10.0", "Polymer.Codegen.Protobuf.dll"),
                Path.Combine(pluginDirectory, "Release", "net10.0", "Polymer.Codegen.Protobuf.dll")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            var matches = Directory.EnumerateFiles(pluginDirectory, "Polymer.Codegen.Protobuf.dll", SearchOption.AllDirectories)
                .Where(path => path.Contains($"{Path.DirectorySeparatorChar}net10.0{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();

            if (matches.Count == 0)
            {
                throw new FileNotFoundException("Unable to locate Polymer.Codegen.Protobuf.dll. Ensure the project was built.");
            }

            return matches[0];
        }
    }

    private static class TestPath
    {
        public static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        public static string Combine(params string[] segments) => Path.Combine(new[] { Root }.Concat(segments).ToArray());
    }
}
