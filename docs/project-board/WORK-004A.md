# WORK-004A – In-Proc Host Package

## Goal
Ship OmniRelay in-proc host as a NuGet package with embedded capability manifest.

## Scope
- Host builder API for services; sample wiring.
- Generate capability manifest at publish.
- AOT publish for linux-x64/arm64; macOS dev build for validation.

## Acceptance Criteria
- Package installs into sample service; starts with provided config.
- Capability manifest emitted and readable by MeshKit.

## Status
Open
