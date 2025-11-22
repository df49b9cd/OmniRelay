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
Open
