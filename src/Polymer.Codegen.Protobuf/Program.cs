using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Compiler;
using Google.Protobuf.Reflection;

namespace Polymer.Codegen.Protobuf;

internal static class Program
{
    private static int Main()
    {
        CodeGeneratorRequest request;
        try
        {
            request = CodeGeneratorRequest.Parser.ParseFrom(Console.OpenStandardInput());
        }
        catch (Exception ex)
        {
            var errorResponse = new CodeGeneratorResponse
            {
                Error = $"Failed to parse CodeGeneratorRequest: {ex.Message}"
            };
            errorResponse.WriteTo(Console.OpenStandardOutput());
            return 1;
        }

        var generator = new ProtoGenerator(request);
        var response = generator.Generate();
        using var codedOutput = new CodedOutputStream(Console.OpenStandardOutput());
        response.WriteTo(codedOutput);
        codedOutput.Flush();
        return 0;
    }
}

internal sealed class ProtoGenerator
{
    private readonly CodeGeneratorRequest _request;
    private readonly Dictionary<string, ProtoFileContext> _fileContexts;
    private readonly Dictionary<string, TypeInfo> _typeLookup;

    public ProtoGenerator(CodeGeneratorRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _fileContexts = BuildFileContexts(request);
        _typeLookup = BuildTypeLookup(_fileContexts);
    }

    public CodeGeneratorResponse Generate()
    {
        var response = new CodeGeneratorResponse
        {
            SupportedFeatures = (ulong)CodeGeneratorResponse.Types.Feature.Proto3Optional
        };

        foreach (var fileName in _request.FileToGenerate)
        {
            if (!_fileContexts.TryGetValue(fileName, out var context))
            {
                response.Error = $"Unable to locate descriptor for proto '{fileName}'.";
                return response;
            }

            if (context.Services.Count == 0)
            {
                continue;
            }

            foreach (var service in context.Services)
            {
                var generator = new ServiceGenerator(context, service, _typeLookup);
                var generatedFile = generator.Generate();
                response.File.Add(generatedFile);
            }
        }

        return response;
    }

    private static Dictionary<string, ProtoFileContext> BuildFileContexts(CodeGeneratorRequest request)
    {
        var map = new Dictionary<string, ProtoFileContext>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in request.ProtoFile)
        {
            var context = new ProtoFileContext(file, ResolveNamespace(file));
            map[file.Name] = context;
        }

        return map;
    }

    private static Dictionary<string, TypeInfo> BuildTypeLookup(Dictionary<string, ProtoFileContext> contexts)
    {
        var lookup = new Dictionary<string, TypeInfo>(StringComparer.Ordinal);

        foreach (var context in contexts.Values)
        {
            foreach (var (protoName, csharpName) in context.TypeNameMap)
            {
                lookup[protoName] = new TypeInfo(context.Namespace, csharpName);
            }
        }

        return lookup;
    }

    private static string ResolveNamespace(FileDescriptorProto file)
    {
        var optionNamespace = file.Options?.CsharpNamespace;
        if (!string.IsNullOrWhiteSpace(optionNamespace))
        {
            return optionNamespace!;
        }

        if (string.IsNullOrWhiteSpace(file.Package))
        {
            return "Protobuf";
        }

        var segments = file.Package.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizeIdentifier)
            .Select(ToPascalCase);
        return string.Join('.', segments);
    }

    private static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else
            {
                builder.Append('_');
            }
        }

        return builder.ToString();
    }

    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var parts = value.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0)
            {
                continue;
            }

            builder.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                builder.Append(part.AsSpan(1));
            }
        }

        return builder.Length == 0 ? value : builder.ToString();
    }
}

internal sealed class ProtoFileContext
{
    public ProtoFileContext(FileDescriptorProto file, string @namespace)
    {
        File = file ?? throw new ArgumentNullException(nameof(file));
        Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
        Services = file.Service.ToList();
        TypeNameMap = BuildTypeNameMap(file);
    }

    public FileDescriptorProto File { get; }
    public string Namespace { get; }
    public IReadOnlyList<ServiceDescriptorProto> Services { get; }
    public IReadOnlyDictionary<string, string> TypeNameMap { get; }

    private Dictionary<string, string> BuildTypeNameMap(FileDescriptorProto file)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var prefix = string.IsNullOrWhiteSpace(file.Package) ? string.Empty : file.Package;

        foreach (var message in file.MessageType)
        {
            CollectMessageTypes(message, prefix, null, map);
        }

        return map;
    }

    private static void CollectMessageTypes(
        DescriptorProto message,
        string? protoPrefix,
        string? csharpPrefix,
        IDictionary<string, string> map)
    {
        var protoName = string.IsNullOrEmpty(protoPrefix)
            ? message.Name
            : string.Concat(protoPrefix, ".", message.Name);

        var csharpName = string.IsNullOrEmpty(csharpPrefix)
            ? message.Name
            : string.Concat(csharpPrefix, ".Types.", message.Name);

        map[protoName] = csharpName;

        foreach (var nested in message.NestedType)
        {
            CollectMessageTypes(nested, protoName, csharpName, map);
        }
    }
}

internal sealed record TypeInfo(string Namespace, string CsharpName);

internal sealed class ServiceGenerator
{
    private readonly ProtoFileContext _fileContext;
    private readonly ServiceDescriptorProto _service;
    private readonly Dictionary<string, TypeInfo> _typeLookup;

    public ServiceGenerator(
        ProtoFileContext fileContext,
        ServiceDescriptorProto service,
        Dictionary<string, TypeInfo> typeLookup)
    {
        _fileContext = fileContext ?? throw new ArgumentNullException(nameof(fileContext));
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _typeLookup = typeLookup ?? throw new ArgumentNullException(nameof(typeLookup));
    }

    public CodeGeneratorResponse.Types.File Generate()
    {
        var builder = new IndentedStringBuilder();
        builder.AppendLine("// <auto-generated>");
        builder.AppendLine("// Generated by protoc-gen-polymer-csharp. Do not edit.");
        builder.AppendLine("// </auto-generated>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Threading;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine("using Polymer.Core;");
        builder.AppendLine("using Polymer.Core.Clients;");
        builder.AppendLine("using Polymer.Core.Transport;");
        builder.AppendLine("using Hugo;");
        builder.AppendLine("using Polymer.Dispatcher;");
        builder.AppendLine();
        builder.AppendLine($"namespace {_fileContext.Namespace};");
        builder.AppendLine();

        var className = GetStaticClassName();
        builder.AppendLine($"public static class {className}");
        builder.AppendLine("{");
        builder.PushIndent();

        var methods = BuildMethodModels();
        foreach (var method in methods)
        {
            builder.AppendLine($"private static readonly ProtobufCodec<{method.InputType}, {method.OutputType}> {method.CodecFieldName} = new(defaultEncoding: ProtobufEncoding.Protobuf);");
        }

        builder.AppendLine();
        GenerateRegisterMethod(builder, methods);
        builder.AppendLine();
        GenerateInterface(builder, methods);
        builder.AppendLine();
        GenerateClientClass(builder, methods);
        builder.AppendLine();
        GenerateClientFactory(builder);
        builder.PopIndent();
        builder.AppendLine("}");

        var fileName = BuildOutputFileName();
        return new CodeGeneratorResponse.Types.File
        {
            Name = fileName,
            Content = builder.ToString()
        };
    }

    private string BuildOutputFileName()
    {
        var baseName = Path.GetFileNameWithoutExtension(_fileContext.File.Name);
        var directory = Path.GetDirectoryName(_fileContext.File.Name);
        var fileName = $"{baseName}.{_service.Name}.Polymer.g.cs";
        if (string.IsNullOrEmpty(directory))
        {
            return fileName.Replace('\\', '/');
        }

        return Path.Combine(directory, fileName).Replace('\\', '/');
    }

    private string GetStaticClassName() => $"{_service.Name}Polymer";

    private List<MethodModel> BuildMethodModels()
    {
        var models = new List<MethodModel>(_service.Method.Count);
        foreach (var method in _service.Method)
        {
            var kind = RpcKind.Unary;
            if (method.ClientStreaming && method.ServerStreaming)
            {
                kind = RpcKind.DuplexStreaming;
            }
            else if (method.ClientStreaming)
            {
                kind = RpcKind.ClientStreaming;
            }
            else if (method.ServerStreaming)
            {
                kind = RpcKind.ServerStreaming;
            }

            var inputType = ResolveClrType(method.InputType);
            var outputType = ResolveClrType(method.OutputType);
            var methodName = method.Name;
            var handlerName = methodName + "Async";
            var sanitized = SanitizeIdentifier(methodName);

            var codecField = $"__{sanitized}Codec";
            var unaryField = $"_{ToCamelCase(methodName)}UnaryClient";
            var streamField = $"_{ToCamelCase(methodName)}StreamClient";
            var clientStreamField = $"_{ToCamelCase(methodName)}ClientStream";
            var duplexField = $"_{ToCamelCase(methodName)}DuplexClient";

            models.Add(new MethodModel(
                methodName,
                methodName,
                inputType,
                outputType,
                kind,
                handlerName,
                codecField,
                unaryField,
                streamField,
                clientStreamField,
                duplexField));
        }

        return models;
    }

    private string ResolveClrType(string protoType)
    {
        var normalized = protoType.TrimStart('.');
        if (!_typeLookup.TryGetValue(normalized, out var info))
        {
            throw new InvalidOperationException($"Unknown proto type '{protoType}' referenced by {_service.Name}.");
        }

        return $"global::{info.Namespace}.{info.CsharpName}";
    }

    private void GenerateRegisterMethod(IndentedStringBuilder builder, IReadOnlyList<MethodModel> methods)
    {
        builder.AppendLine($"public static void Register{_service.Name}(this global::Polymer.Dispatcher.Dispatcher dispatcher, I{_service.Name} implementation)");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("ArgumentNullException.ThrowIfNull(dispatcher);");
        builder.AppendLine("ArgumentNullException.ThrowIfNull(implementation);");
        builder.AppendLine();

        foreach (var method in methods)
        {
            switch (method.Kind)
            {
                case RpcKind.Unary:
                    builder.AppendLine($"dispatcher.RegisterUnary(\"{method.ProcedureName}\", builder =>");
                    builder.AppendLine("{");
                    builder.PushIndent();
                    builder.AppendLine($"builder.WithEncoding({method.CodecFieldName}.Encoding);");
                    builder.AppendLine($"builder.Handle(ProtobufCallAdapters.CreateUnaryHandler({method.CodecFieldName}, implementation.{method.HandlerName}));");
                    builder.PopIndent();
                    builder.AppendLine("});");
                    break;
                case RpcKind.ServerStreaming:
                    builder.AppendLine($"dispatcher.RegisterStream(\"{method.ProcedureName}\", builder =>");
                    builder.AppendLine("{");
                    builder.PushIndent();
                    builder.AppendLine($"builder.WithEncoding({method.CodecFieldName}.Encoding);");
                    builder.AppendLine($"builder.Handle(ProtobufCallAdapters.CreateServerStreamHandler({method.CodecFieldName}, implementation.{method.HandlerName}));");
                    builder.PopIndent();
                    builder.AppendLine("});");
                    break;
                case RpcKind.ClientStreaming:
                    builder.AppendLine($"dispatcher.RegisterClientStream(\"{method.ProcedureName}\", builder =>");
                    builder.AppendLine("{");
                    builder.PushIndent();
                    builder.AppendLine($"builder.WithEncoding({method.CodecFieldName}.Encoding);");
                    builder.AppendLine($"builder.Handle(ProtobufCallAdapters.CreateClientStreamHandler({method.CodecFieldName}, implementation.{method.HandlerName}));");
                    builder.PopIndent();
                    builder.AppendLine("});");
                    break;
                case RpcKind.DuplexStreaming:
                    builder.AppendLine($"dispatcher.RegisterDuplex(\"{method.ProcedureName}\", builder =>");
                    builder.AppendLine("{");
                    builder.PushIndent();
                    builder.AppendLine($"builder.WithEncoding({method.CodecFieldName}.Encoding);");
                    builder.AppendLine($"builder.Handle(ProtobufCallAdapters.CreateDuplexHandler({method.CodecFieldName}, implementation.{method.HandlerName}));");
                    builder.PopIndent();
                    builder.AppendLine("});");
                    break;
            }
            builder.AppendLine();
        }

        builder.PopIndent();
        builder.AppendLine("}");
    }

    private void GenerateInterface(IndentedStringBuilder builder, IReadOnlyList<MethodModel> methods)
    {
        builder.AppendLine($"public interface I{_service.Name}");
        builder.AppendLine("{");
        builder.PushIndent();

        foreach (var method in methods)
        {
            var signature = method.Kind switch
            {
                RpcKind.Unary => $"ValueTask<Response<{method.OutputType}>> {method.HandlerName}(Request<{method.InputType}> request, CancellationToken cancellationToken)",
                RpcKind.ServerStreaming => $"ValueTask {method.HandlerName}(Request<{method.InputType}> request, ProtobufCallAdapters.ProtobufServerStreamWriter<{method.InputType}, {method.OutputType}> stream, CancellationToken cancellationToken)",
                RpcKind.ClientStreaming => $"ValueTask<Response<{method.OutputType}>> {method.HandlerName}(ProtobufCallAdapters.ProtobufClientStreamContext<{method.InputType}, {method.OutputType}> context, CancellationToken cancellationToken)",
                RpcKind.DuplexStreaming => $"ValueTask {method.HandlerName}(ProtobufCallAdapters.ProtobufDuplexStreamContext<{method.InputType}, {method.OutputType}> context, CancellationToken cancellationToken)",
                _ => throw new ArgumentOutOfRangeException()
            };

            builder.AppendLine($"{signature};");
        }

        builder.PopIndent();
        builder.AppendLine("}");
    }

    private void GenerateClientClass(IndentedStringBuilder builder, IReadOnlyList<MethodModel> methods)
    {
        builder.AppendLine($"public sealed class {_service.Name}Client");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("private readonly global::Polymer.Dispatcher.Dispatcher _dispatcher;");
        builder.AppendLine("private readonly string _service;");
        builder.AppendLine("private readonly string? _outboundKey;");

        foreach (var method in methods)
        {
            switch (method.Kind)
            {
                case RpcKind.Unary:
                    builder.AppendLine($"private UnaryClient<{method.InputType}, {method.OutputType}>? {method.UnaryClientField};");
                    break;
                case RpcKind.ServerStreaming:
                    builder.AppendLine($"private StreamClient<{method.InputType}, {method.OutputType}>? {method.StreamClientField};");
                    break;
                case RpcKind.ClientStreaming:
                    builder.AppendLine($"private ClientStreamClient<{method.InputType}, {method.OutputType}>? {method.ClientStreamField};");
                    break;
                case RpcKind.DuplexStreaming:
                    builder.AppendLine($"private DuplexStreamClient<{method.InputType}, {method.OutputType}>? {method.DuplexClientField};");
                    break;
            }
        }

        builder.AppendLine();
        builder.AppendLine($"internal {_service.Name}Client(global::Polymer.Dispatcher.Dispatcher dispatcher, string service, string? outboundKey)");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("ArgumentNullException.ThrowIfNull(dispatcher);");
        builder.AppendLine("ArgumentException.ThrowIfNullOrWhiteSpace(service);");
        builder.AppendLine("_dispatcher = dispatcher;");
        builder.AppendLine("_service = service;");
        builder.AppendLine("_outboundKey = outboundKey;");

        builder.PopIndent();
        builder.AppendLine("}");
        builder.AppendLine();

        foreach (var method in methods)
        {
            switch (method.Kind)
            {
                case RpcKind.Unary:
                    builder.AppendLine($"public ValueTask<Result<Response<{method.OutputType}>>> {method.HandlerName}({method.InputType} request, RequestMeta? meta = null, CancellationToken cancellationToken = default)");
                    builder.AppendLine("{");
                    builder.PushIndent();
                    builder.AppendLine($"var requestMeta = PrepareRequestMeta(meta, _service, \"{method.ProcedureName}\", {method.CodecFieldName}.Encoding);");
                    builder.AppendLine($"var client = {method.UnaryClientField} ??= _dispatcher.CreateUnaryClient<{method.InputType}, {method.OutputType}>(_service, {method.CodecFieldName}, _outboundKey);");
                    builder.AppendLine($"return client.CallAsync(new Request<{method.InputType}>(requestMeta, request), cancellationToken);");
                    builder.PopIndent();
                    builder.AppendLine("}");
                    builder.AppendLine();
                    break;
                case RpcKind.ServerStreaming:
                    builder.AppendLine($"public IAsyncEnumerable<Response<{method.OutputType}>> {method.HandlerName}({method.InputType} request, RequestMeta? meta = null, CancellationToken cancellationToken = default)");
                    builder.AppendLine("{");
                    builder.PushIndent();
                    builder.AppendLine($"var requestMeta = PrepareRequestMeta(meta, _service, \"{method.ProcedureName}\", {method.CodecFieldName}.Encoding);");
                    builder.AppendLine($"var client = {method.StreamClientField} ??= _dispatcher.CreateStreamClient<{method.InputType}, {method.OutputType}>(_service, {method.CodecFieldName}, _outboundKey);");
                    builder.AppendLine($"return client.CallAsync(new Request<{method.InputType}>(requestMeta, request), new StreamCallOptions(StreamDirection.Server), cancellationToken);");
                    builder.PopIndent();
                    builder.AppendLine("}");
                    builder.AppendLine();
                    break;
                case RpcKind.ClientStreaming:
                    builder.AppendLine($"public ValueTask<ClientStreamClient<{method.InputType}, {method.OutputType}>.ClientStreamSession> {method.HandlerName}(RequestMeta? meta = null, CancellationToken cancellationToken = default)");
                    builder.AppendLine("{");
                    builder.PushIndent();
                    builder.AppendLine($"var requestMeta = PrepareRequestMeta(meta, _service, \"{method.ProcedureName}\", {method.CodecFieldName}.Encoding);");
                    builder.AppendLine($"var client = {method.ClientStreamField} ??= _dispatcher.CreateClientStreamClient<{method.InputType}, {method.OutputType}>(_service, {method.CodecFieldName}, _outboundKey);");
                    builder.AppendLine($"return client.StartAsync(requestMeta, cancellationToken);");
                    builder.PopIndent();
                    builder.AppendLine("}");
                    builder.AppendLine();
                    break;
                case RpcKind.DuplexStreaming:
                    builder.AppendLine($"public ValueTask<DuplexStreamClient<{method.InputType}, {method.OutputType}>.DuplexStreamSession> {method.HandlerName}(RequestMeta? meta = null, CancellationToken cancellationToken = default)");
                    builder.AppendLine("{");
                    builder.PushIndent();
                    builder.AppendLine($"var requestMeta = PrepareRequestMeta(meta, _service, \"{method.ProcedureName}\", {method.CodecFieldName}.Encoding);");
                    builder.AppendLine($"var client = {method.DuplexClientField} ??= _dispatcher.CreateDuplexStreamClient<{method.InputType}, {method.OutputType}>(_service, {method.CodecFieldName}, _outboundKey);");
                    builder.AppendLine($"return client.StartAsync(requestMeta, cancellationToken);");
                    builder.PopIndent();
                    builder.AppendLine("}");
                    builder.AppendLine();
                    break;
            }
        }

        builder.AppendLine("private static RequestMeta PrepareRequestMeta(RequestMeta? meta, string service, string procedure, string encoding)");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("if (meta is null)");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("return new RequestMeta(service: service, procedure: procedure, encoding: encoding);");
        builder.PopIndent();
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("var value = meta;");
        builder.AppendLine("if (string.IsNullOrWhiteSpace(value.Service))");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("value = value with { Service = service };");
        builder.PopIndent();
        builder.AppendLine("}");
        builder.AppendLine("if (string.IsNullOrWhiteSpace(value.Procedure))");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("value = value with { Procedure = procedure };");
        builder.PopIndent();
        builder.AppendLine("}");
        builder.AppendLine("if (string.IsNullOrWhiteSpace(value.Encoding))");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("value = value with { Encoding = encoding };");
        builder.PopIndent();
        builder.AppendLine("}");
        builder.AppendLine("return value;");
        builder.PopIndent();
        builder.AppendLine("}");

        builder.PopIndent();
        builder.AppendLine("}");
    }

    private void GenerateClientFactory(IndentedStringBuilder builder)
    {
        builder.AppendLine($"public static {_service.Name}Client Create{_service.Name}Client(global::Polymer.Dispatcher.Dispatcher dispatcher, string service, string? outboundKey = null)");
        builder.AppendLine("{");
        builder.PushIndent();
        builder.AppendLine("ArgumentNullException.ThrowIfNull(dispatcher);");
        builder.AppendLine("ArgumentException.ThrowIfNullOrWhiteSpace(service);");
        builder.AppendLine($"return new {_service.Name}Client(dispatcher, service, outboundKey);");
        builder.PopIndent();
        builder.AppendLine("}");
    }

    private static string SanitizeIdentifier(string identifier)
    {
        var builder = new StringBuilder(identifier.Length);
        foreach (var ch in identifier)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }

        return builder.ToString();
    }

    private static string ToCamelCase(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return identifier;
        }

        if (identifier.Length == 1)
        {
            return identifier.ToLowerInvariant();
        }

        return char.ToLowerInvariant(identifier[0]) + identifier.Substring(1);
    }
}

internal enum RpcKind
{
    Unary,
    ServerStreaming,
    ClientStreaming,
    DuplexStreaming
}

internal sealed record MethodModel(
    string Name,
    string ProcedureName,
    string InputType,
    string OutputType,
    RpcKind Kind,
    string HandlerName,
    string CodecFieldName,
    string UnaryClientField,
    string StreamClientField,
    string ClientStreamField,
    string DuplexClientField);

internal sealed class IndentedStringBuilder
{
    private readonly StringBuilder _builder = new();
    private int _indentLevel;
    private const string Indent = "    ";

    public void PushIndent() => _indentLevel++;
    public void PopIndent()
    {
        if (_indentLevel > 0)
        {
            _indentLevel--;
        }
    }

    public void AppendLine() => _builder.AppendLine();

    public void AppendLine(string text)
    {
        if (text.Length == 0)
        {
            _builder.AppendLine();
            return;
        }

        for (var i = 0; i < _indentLevel; i++)
        {
            _builder.Append(Indent);
        }

        _builder.AppendLine(text);
    }

    public override string ToString() => _builder.ToString();
}
