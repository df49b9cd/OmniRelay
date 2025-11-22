# WORK-009 – Epic: Bootstrap & Watch Harness

Split into iteration-sized stories (A–C).

## Child Stories
- **WORK-009A** – Deterministic startup pipeline (LKG → stage → activate)
- **WORK-009B** – Watch lifecycle (backoff/resume/state machine)
- **WORK-009C** – Validation hooks & observability

## Definition of Done (epic)
- Shared harness used by all hosts/roles; startup and watch flows observable and resilient.

## Testing Strategy
- Unit: Cover new logic/config parsing/helpers introduced by this item.
- Integration: Exercise end-to-end behavior via test fixtures (hosts/agents/registry) relevant to this item.
- Feature: Scenario-level validation of user-visible workflows touched by this item across supported deployment modes/roles.
- Hyperscale: Run when the change affects runtime/throughput/scale; otherwise note non-applicability with rationale in the PR.
