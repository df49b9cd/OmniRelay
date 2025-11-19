# WORK-017 – Samples & Documentation Refresh

## Goal
Update samples, READMEs, and diagrams so new contributors can stand up the Hugo → OmniRelay → MeshKit stack, run CLI workflows, and understand which layer owns which responsibility.

## Scope
- Refresh `samples/ResourceLease.MeshDemo` (and related docker assets) to show OmniRelay transports hosting MeshKit control-plane modules.
- Revise docs in `docs/knowledge-base` and `docs/reference` covering onboarding, HTTP/3 diagnostics, MeshKit modules, and CLI usage.
- Add diagrams/flowcharts depicting traffic flow, bootstrapping, and operator workflows under the new architecture.
- Provide validation checklists ensuring instructions succeed end-to-end on macOS/Linux/Windows.

## Requirements
1. **Accuracy** – Terminology and steps must align with `transport-layer-vision.md` and current CLI verbs.
2. **Examples** – Include step-by-step instructions for enabling HTTP/3, running MeshKit.Shards APIs, executing rebalancer workflows, and observing telemetry/dashboards.
3. **Automation** – Integrate docs linting, link checking, and CLI snippet validation in CI.
4. **Screenshots/logs** – Offer refreshed screenshots/log extracts for CLI + dashboards after new flows.
5. **AOT** – Document how to run native AOT builds of OmniRelay + MeshKit components.

## Deliverables
- Updated sample configs/compose files.
- Revised documentation + diagrams.
- Verification checklist/test plan covering the documented workflows.

## Acceptance Criteria
- Following the refreshed docs enables a new engineer to boot the layered stack, run CLI commands, and inspect telemetry without guessing.
- HTTP/3 defaults + downgrades are explained with CLI + dashboard validation steps.
- Docs lint/tests pass in CI; stakeholders sign off.

## Testing Strategy
- Automated docs linting/link checking + snippet validation.
- Integration tests exercising sample instructions on supported OS/runner combos.
- Feature tests: buddy-testing sessions verifying docs clarity.
- Hyperscale tests: ensure multi-OS instructions stay consistent.

## References
- `docs/architecture/transport-layer-vision.md`
- `docs/project-board/transport-layer-plan.md`