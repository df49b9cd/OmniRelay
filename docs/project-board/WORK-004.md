# WORK-004 – Native AOT Tooling & Packaging

## Goal
Distribute OmniRelay CLI and supporting tooling as native AOT binaries for linux-x64, linux-arm64, macOS, and Windows, minimizing cold-start time and dependency footprint for cloud-native workflows.

## Scope
- Update CLI/tooling projects to be trimming/AOT safe (source-generated command parsers, serialization contexts, DI usage) and configure publish profiles for each RID.
- Produce self-contained binaries and/or slim container images; update dotnet tool packages to include or reference AOT builds.
- Document installation paths, platform support, troubleshooting, and fallback instructions.

## Requirements
1. **CLI AOT builds** – `dotnet publish OmniRelay.Cli.csproj /p:PublishAot=true` succeeds for target RIDs with zero warnings.
2. **Command parsing** – Use source generators (System.CommandLine or custom) to avoid reflection.
3. **Packaging** – Provide scripts to package binaries into dotnet tool nupkgs, tarballs/zip files, and optionally distroless containers.
4. **Telemetry/auth** – Ensure CLI authentication helpers and telemetry remain functional and trimmed.
5. **Docs** – Update CLI docs referencing AOT distribution, supported platforms, fallback to JIT when needed.

## Deliverables
- Build scripts/pipelines producing native binaries + container images.
- Documentation for installing/upgrading AOT CLI.
- Smoke-test suite validating commands on each platform.

## Acceptance Criteria
- CLI runs as native AOT on supported platforms executing core commands successfully.
- Dotnet tool/container distributions published as part of release pipeline.
- Observed startup latency reduction recorded for release notes.

## Testing Strategy
- Unit: command parser generators, trimming-safe helpers.
- Integration: run published binaries for each platform executing representative CLI commands.
- Feature: incorporate AOT CLI in feature/hyperscale automation to validate long-running scenarios.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`