# WORK-003 – MeshKit Library AOT Readiness

## Goal
Make every MeshKit module (shards, registry, rebalancer, cluster descriptors, failover, observability) trimming/AOT-friendly so control-plane services can publish as native AOT without bespoke workarounds.

## Scope
- Audit MeshKit libraries for trimming blockers (reflection, dynamically loaded assemblies, unsupported configuration patterns).
- Introduce source-generated registries (middleware/interceptors, event handlers) rather than runtime scanning.
- Ensure DI/configuration binding avoids non-trimmable patterns (`Activator.CreateInstance`, unbounded reflection) and annotate unavoidable cases.
- Provide analyzers/build checks to prevent regressions.

## Requirements
1. **Library trimming** – Each MeshKit assembly builds with `<PublishTrimmed>true` and zero warnings.
2. **Source-generated metadata** – Replace runtime discovery with generators for registries and configuration maps.
3. **Configuration binding** – Use trimming-friendly binding (OptionsBuilder) with annotations.
4. **Analyzer** – CI fails if new code introduces disallowed APIs.
5. **Docs** – Update module READMEs indicating AOT support/constraints.

## Deliverables
- Code changes, generators, annotations, analyzer/build tooling, documentation.

## Acceptance Criteria
- Representative MeshKit services (shards, rebalancer, leadership) publish/run as native AOT binaries.
- Analyzer prevents new reflection-based patterns from landing.
- Documentation describes AOT expectations per module.

## Testing Strategy
- Unit: generator tests, trimming-targeted tests.
- Integration: publish representative MeshKit hosts as AOT and run integration suites.
- Feature/Hyperscale: run MeshKit scenarios on AOT builds to ensure parity.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`