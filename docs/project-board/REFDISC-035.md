# REFDISC-035 - Control-Plane Library AOT Readiness

## Goal
Make every shared control-plane kit (transport builders, diagnostics runtime, telemetry, leadership, etc.) AOT-friendly by removing reflection dependencies, enabling trimming, and adding source generators so control-plane services can publish as native AOT without custom workarounds.

## Scope
- Audit each library under `src/OmniRelay.Core`, `OmniRelay.Transport`, `OmniRelay.Diagnostics`, and future REFDISC modules for trimming/AOT blockers.
- Introduce generator-based registries (e.g., middleware/interceptor lists) instead of runtime scanning.
- Ensure DI registrations and configuration binding avoid `Activator.CreateInstance` or unsupported patterns.
- Provide shared analyzers or Roslyn checks to catch AOT regressions in new libraries.

## Requirements
1. **Library trimming** - Each shared library must compile with `<PublishTrimmed>true` and include necessary `DynamicallyAccessedMembers` annotations.
2. **Source-generated metadata** - Replace runtime enumeration (attributes, reflection scanning) with generator-produced registries.
3. **Configuration binding** - Use trimming-friendly binding (`OptionsBuilder.ValidateOnStart`, `IOptions<T>` with known members) and annotate types accordingly.
4. **Analyzer** - Add an analyzer or build step that fails if new libraries introduce disallowed APIs (reflection emit, `Assembly.Load`).
5. **Docs** - Update per-library documentation indicating AOT support and any constraints.

## Deliverables
- Code changes across control-plane libraries ensuring trimming/AOT compatibility (source generators, annotations).
- Analyzer/build tooling to enforce allowed API usage.
- Documentation updates (README + module docs) describing AOT readiness.
- Example control-plane host published as native AOT (gossip/leadership) verifying library compatibility.

## Acceptance Criteria
- Each control-plane library builds with trimming warnings treated as errors (0 warnings).
- A sample control-plane service (gossip host) publishes/runs as native AOT using the shared kits.
- Analyzer catches intentional regressions during code review/CI.
- Documentation reflects AOT limitations (if any) and mitigation strategies.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Add generator-focused tests to ensure produced registries match expected middleware/interceptor lists.
- Validate configuration binding works when properties are trimmed (use `TrimmingTest` patterns).

### Integration tests
- Publish representative control-plane hosts (gossip, leadership, diagnostics) as native AOT binaries and run integration scenarios.
- Verify DI/service registration functions identically in AOT vs. JIT builds.

### Feature tests
- Execute OmniRelay.FeatureTests targeting control-plane services compiled via AOT to ensure operator workflows remain intact.
- Confirm CLI/diagnostics interactions remain functional with AOT hosts.

### Hyperscale Feature Tests
- Deploy native AOT control-plane services in the hyperscale suite, stressing gossip/leadership operations to ensure no trimming-induced regressions.
- Monitor startup latency and memory to confirm expected AOT characteristics.

## References
- Newly added REFDISC transport/diagnostics kits (e.g., REFDISC-001…013) - ensure each is covered.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
