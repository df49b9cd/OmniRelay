# REFDISC-034 - AOT Compliance Baseline for Core Runtime

## Goal
Establish OmniRelay’s dispatcher and runtime libraries as native AOT-first by auditing and refactoring code paths to eliminate trimming blockers, dynamic code generation, and reflection-heavy patterns so they compile cleanly with `PublishAot`.

## Scope
- Inventory AOT blockers across `src/OmniRelay` (reflection, dynamic assemblies, non-trimmable dependencies).
- Introduce source generators or `DynamicDependency` hints where reflection is unavoidable (e.g., serializer contexts, DI activators).
- Ensure transport hosts, middleware, and diagnostics components avoid runtime code emit/expression trees incompatible with AOT.
- Document AOT-safe coding guidelines for contributors.

## Requirements
1. **Compile cleanly** - `dotnet publish -r linux-x64 -c Release /p:PublishAot=true OmniRelay.slnx` must succeed without warnings suppressed.
2. **Trimming safe** - All shipped assemblies must enable trimming and annotate unreachable code paths appropriately.
3. **No runtime codegen** - Replace expression tree compilation, `Reflection.Emit`, or runtime IL generation with compile-time alternatives.
4. **Serializer coverage** - Use source-generated serializers for control-plane payloads (JSON, gRPC metadata) to avoid reflection-based serialization.
5. **Documentation** - Provide contributor guidance on writing trimming/AOT-safe code and reviewing new dependencies.

## Deliverables
- AOT compliance report (before/after) listing resolved blockers.
- Code changes replacing reflection/codegen-heavy sections with AOT-safe alternatives or adding source generators.
- Contributor docs (`docs/architecture/aot-guidelines.md`) outlining requirements.
- CI script to run native AOT builds during validation (handed off to REFDISC-037 for ongoing enforcement).

## Acceptance Criteria
- Dispatcher publishes as native AOT with zero warnings and passes smoke tests.
- All runtime libraries enable trimming and have necessary annotations.
- Reflection usage is either removed or justified with explicit annotations/source generators.
- Documentation reviewed/approved by runtime owners.

- Native AOT gate: Publish with /p:PublishAot=true and treat trimming warnings as errors per REFDISC-034..037.

## Testing Strategy
All test tiers must run against native AOT artifacts per REFDISC-034..037.


### Unit tests
- Add targeted tests for newly introduced source generators or compile-time bindings to ensure parity with previous reflection-based behavior.
- Validate guards that replace reflection (e.g., switch expressions) behave identically under trimming.

### Integration tests
- Publish dispatcher as AOT, run integration suites (gossip, leadership) against the native binary to ensure functionality.
- Verify logging/diagnostics endpoints work under native AOT builds.

### Feature tests
- Execute OmniRelay.FeatureTests with the AOT build to confirm end-to-end workflows (RPC routing, control-plane operations) remain stable.
- Ensure CLI/operator tooling interacts correctly with the AOT-hosted dispatcher.

### Hyperscale Feature Tests
- Run OmniRelay.HyperscaleFeatureTests against native AOT builds to validate performance/scalability characteristics match or exceed JIT builds.
- Monitor resource usage (memory, startup time) to confirm AOT benefits are realized without regressions.

## References
- `.NET Native AOT docs` (dotnet/runtime) - best practices for trimming and source generators.
- Existing serializer contexts (e.g., `MeshGossipJsonSerializerContext`) as examples.
- REFDISC-034..037 - AOT readiness baseline and CI gating.
