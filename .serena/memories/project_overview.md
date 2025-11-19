## Purpose
OmniRelay is a .NET 10 port of Uber's YARPC runtime built on Hugo primitives, offering transports, middleware, routing, diagnostics, and tooling for RPC workloads. Assemblies/packages publish under OmniRelay.*.

## Tech Stack
- Language: C# (.NET 10)
- Build/test: dotnet SDK 10, xUnit
- Tooling: System.CommandLine-based CLI, Roslyn incremental generator, Protobuf plugin
- Containers: Docker recipes for CI/hyperscale smoke, AOT publish helpers
- Host environment: macOS (Darwin) with zsh shell

## Codebase Structure (high level)
- src/OmniRelay: dispatcher core, transports, codecs, middleware, peers, clients
- src/OmniRelay.Configuration: configuration binder/AddOmniRelayDispatcher
- src/OmniRelay.Cli: `omnirelay` CLI
- src/OmniRelay.Codegen.*: Protobuf `protoc` plugin + Roslyn generator
- tests/OmniRelay.Core.UnitTests: primary unit tests
- tests/OmniRelay.YabInterop: yab HTTP/gRPC interop harness
- docs/: architecture, AOT/diagnostics/guides; samples/: runnable demos
- eng/: scripts (run-ci.sh, run-aot-publish.sh, run-hyperscale-smoke.sh); docker/: CI and hyperscale Dockerfiles

## Key Entry Points
- CLI: `dotnet run --project src/OmniRelay.Cli -- --help`
- Library usage: AddOmniRelayDispatcher in host; dispatcher/options examples in README

## Notable Features
Transports (HTTP/gRPC), codecs (JSON/Protobuf/raw), middleware suite (tracing/metrics/deadlines/retries/etc.), peer choosers and sharding helpers, ResourceLease mesh components, diagnostics endpoints (/omnirelay/introspect, /healthz, /readyz), codegen tools, AOT-first guidance.
