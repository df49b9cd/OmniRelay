# Protobuf Code Generation

Polymer ships a protoc plug-in, `protoc-gen-polymer-csharp`, that generates dispatcher registration helpers and typed clients on top of the runtime `ProtobufCodec`. This document outlines how to run the generator and how to consume the emitted code.

## Building the plug-in

The plug-in lives at `src/Polymer.Codegen.Protobuf/`. It is built automatically when you run `dotnet build` for the repository. The compiled assembly can be found under `src/Polymer.Codegen.Protobuf/bin/<Configuration>/net10.0/Polymer.Codegen.Protobuf.dll`.

If you need a self-contained binary, publish the project:

```bash
cd src/Polymer.Codegen.Protobuf
 dotnet publish -c Release
```

## Invoking protoc

Because the repo already depends on `Grpc.Tools`, you can add the plug-in invocation to a project file:

```xml
<ItemGroup>
  <PackageReference Include="Grpc.Tools" Version="2.71.0" />
  <Protobuf Include="Protos/test_service.proto" GrpcServices="None">
    <Generator>PolymerCSharp</Generator>
  </Protobuf>
</ItemGroup>

<Target Name="PolymerCodegen" BeforeTargets="BeforeCompile">
  <Exec Command="$(Protobuf_ProtocPath) --plugin=protoc-gen-PolymerCSharp=$(SolutionDir)src/Polymer.Codegen.Protobuf/bin/$(Configuration)/net10.0/Polymer.Codegen.Protobuf.dll --PolymerCSharp_out=$(ProjectDir)Generated $(ProtoRoot)Protos/test_service.proto" />
</Target>
```

Alternatively, call `protoc` directly:

```bash
protoc \
  --plugin=protoc-gen-polymer-csharp=src/Polymer.Codegen.Protobuf/bin/Debug/net10.0/Polymer.Codegen.Protobuf.dll \
  --polymer-csharp_out=Generated \
  --proto_path=Protos \
  Protos/test_service.proto
```

The generated C# file mirrors the proto namespace. See `tests/Polymer.Tests/Generated/TestService.Polymer.g.cs` for a complete example.

## Roslyn incremental generator (MSBuild integration)

For projects that already produce [descriptor sets](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md) you can skip the `protoc` plug-in entirely and let MSBuild feed Polymer’s incremental generator. The generator ships from `src/Polymer.Codegen.Protobuf.Generator/` and can be consumed as a project reference or packaged analyzer.

1. Reference the generator as an analyzer:

   ```xml
   <ItemGroup>
     <ProjectReference Include="..\..\src\Polymer.Codegen.Protobuf.Generator\Polymer.Codegen.Protobuf.Generator.csproj"
                       OutputItemType="Analyzer"
                       ReferenceOutputAssembly="true" />
   </ItemGroup>
   ```

2. Provide a descriptor set (`.pb`) via `AdditionalFiles`. The generator reads binary descriptor sets, so you can pre-generate them with `Grpc.Tools`/`protoc`:

   ```bash
   ~/.nuget/packages/grpc.tools/2.71.0/tools/macosx_x64/protoc \
     --descriptor_set_out=Descriptors/test_service.pb \
     --include_imports \
     --proto_path=Protos \
     Protos/test_service.proto
   ```

   and then include it in the project:

   ```xml
   <ItemGroup>
     <AdditionalFiles Include="Descriptors/test_service.pb" />
   </ItemGroup>
   ```

   You still need the regular `Protobuf` MSBuild item so that DTOs are generated (the incremental generator only produces clients/dispatcher helpers):

   ```xml
   <ItemGroup>
     <Protobuf Include="Protos/test_service.proto" GrpcServices="None" />
   </ItemGroup>
   ```

3. Build the project. MSBuild writes the generated files under `obj/<tfm>/generated/Polymer.Codegen.Protobuf.Generator/...` and the types become available to your project just like the protoc plug-in output.

The repository contains a working sample wired this way: `tests/Polymer.Tests/Projects/ProtobufIncrementalSample/`. It keeps the descriptor set under `Descriptors/test_service.pb`, references the analyzer, and builds successfully with `dotnet build`.

## Runtime integration

Generated code exposes two entry points:

- `TestServicePolymer.RegisterTestService(dispatcher, implementation)` wires the dispatcher to a service implementation that uses strongly-typed requests/responses.
- `TestServicePolymer.CreateTestServiceClient(dispatcher, serviceName, outboundKey)` returns a lazy client that creates `UnaryClient`/`StreamClient`/etc only when the corresponding RPC is invoked.

All generated clients use `ProtobufCodec`, so as long as transports supply a Protobuf outbound the calls will negotiate the correct media type (see `ProtobufCodec` + HTTP metadata handlers).

## Tests

- Golden coverage: `tests/Polymer.Tests/Codegen/ProtobufCodeGeneratorTests.cs`
- Integration coverage: `tests/Polymer.Tests/Codegen/GeneratedServiceIntegrationTests.cs`

These tests regenerate the code for `Protos/test_service.proto`, ensure it matches the checked-in baseline, and exercise unary calls over both HTTP and gRPC.
