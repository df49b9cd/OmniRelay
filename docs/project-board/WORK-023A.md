# WORK-023A – Library Factoring (Transport, Codecs, Protos)

## Goal
Extract shared components into `src/OmniRelay.Transport`, `src/OmniRelay.Codecs`, and `src/OmniRelay.Protos` while keeping OmniRelay as the data-plane core.

## Scope
- Move/commonize transport pipeline, HTTP/gRPC clients/servers, middleware, pooling.
- Factor codecs (encoders/decoders, content negotiation, protobuf JSON/CBOR) into a standalone assembly.
- Establish proto source layout and codegen pipeline for `OmniRelay.Protos`.

## Acceptance Criteria
- Builds succeed with new project structure; OmniRelay still compiles and uses the libraries.
- No behavior change in data-plane tests.

## Status
In progress — shared Codecs/Transport/Protos projects added and OmniRelay now references them; validation pending.

## Testing Strategy
- Unit: Cover new logic/config parsing/helpers introduced by this item.
- Integration: Exercise end-to-end behavior via test fixtures (hosts/agents/registry) relevant to this item.
- Feature: Scenario-level validation of user-visible workflows touched by this item across supported deployment modes/roles.
- Hyperscale: Run when the change affects runtime/throughput/scale; otherwise note non-applicability with rationale in the PR.
