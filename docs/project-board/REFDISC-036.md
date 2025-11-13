# REFDISC-036 - Native AOT Tooling & Packaging

## Goal
Deliver the OmniRelay CLI and auxiliary tooling as native AOT binaries for primary target platforms (linux-x64, linux-arm64, win-x64) to minimize startup time and dependency footprint in cloud-native workflows.

## Scope
- Update `src/OmniRelay.Cli` (and other tools) to be trimming/AOT safe, leveraging source-generated command parsers/serializers.
- Configure publishing scripts to produce native binaries for supported runtimes with optional self-contained containers.
- Ensure installers (dotnet tool, container images) can distribute AOT builds alongside fallback JIT builds.
- Document platform support and troubleshooting guidance.

## Requirements
1. **CLI AOT build** - `dotnet publish OmniRelay.Cli.csproj /p:PublishAot=true` must succeed with zero warnings and pass smoke tests.
2. **Command parsing** - Replace reflection-based option parsing with source generators (e.g., System.CommandLine generators) to stay AOT safe.
3. **Packaging** - Distribute AOT binaries via dotnet tool packages and/or container images with minimal footprint.
4. **Telemetry/auth** - Ensure CLI authentication helpers and telemetry remain functional under AOT builds.
5. **Docs** - Update CLI docs to mention AOT distribution, supported platforms, and fallback instructions.

## Deliverables
- CLI code refactors (source-generated parsers, trimming-safe helpers).
- Build pipeline updates to publish native binaries (packaged artifacts, container samples).
- Documentation covering installation, platform support, and known limitations.
- Smoke-test suite validating AOT CLIs across Linux/Windows.

## Acceptance Criteria
- CLI runs as native AOT on target platforms, executing core commands (peer list, diagnostics) successfully.
- Dotnet tool package includes AOT binaries or provides documented steps to install them.
- Container images shrink due to AOT publishing (< target size).
- Operator feedback confirms CLI responsiveness improvements.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Ensure command parser generators cover all verbs/options and behave identically to previous implementation.
- Validate telemetry/auth helpers function without runtime reflection.

### Integration tests
- Publish CLI as AOT and run integration suites invoking real control-plane APIs.
- Verify self-contained binaries run inside lightweight containers/CI agents.

### Feature tests
- In OmniRelay.FeatureTests automation, replace CLI invocations with AOT binaries and confirm workflows (drain, diagnostics) succeed.
- Measure startup latency improvements to record in release notes.

### Hyperscale Feature Tests
- During OmniRelay.HyperscaleFeatureTests, use the AOT CLI for operator automation scripts to ensure it handles repeated invocations and large output sets without regression.

## References
- System.CommandLine source generator docs, existing CLI commands in `src/OmniRelay.Cli`.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
