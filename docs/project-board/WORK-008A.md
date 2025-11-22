# WORK-008A – LKG Cache & Signature Validation

## Goal
Persist and validate last-known-good config/artifacts for use during partitions.

## Scope
- On-disk cache format with hashes/signatures.
- Load/verify on startup before activation.
- Admin/metrics for cache status.

## Acceptance Criteria
- Corrupted/unsigned cache rejected; healthy cache applied when control is offline.
- Tests cover save/load/validate flows.

## Status
Open
