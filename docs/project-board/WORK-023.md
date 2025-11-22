# WORK-023 – Epic: Shared Transport/Codec/Proto Libraries for MeshKit

Ensure MeshKit reuses OmniRelay’s transport/codec/proto stack via shared packages, avoiding duplicate data-plane logic.

## Child Stories
- **WORK-023A** – Library factoring (Transport, Codecs, Protos)
- **WORK-023B** – NuGet/internal packaging & multi-targeting
- **WORK-023C** – MeshKit integration & regression tests

## Definition of Done (epic)
- OmniRelay exports reusable packages; MeshKit consumes them; no duplicated transport/codec code; AOT safety preserved.
