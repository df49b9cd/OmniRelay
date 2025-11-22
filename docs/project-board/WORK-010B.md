# WORK-010B – Storage & Fetch/Caching Pipeline

## Goal
Implement extension storage (OCI/HTTP) with integrity verification and agent caching.

## Scope
- Upload/download endpoints; hash/signature verification on fetch.
- Agent caching with eviction policy.

## Acceptance Criteria
- Fetch rejects tampered artifacts; cache hits/misses observable.
- Integration tests cover publish->fetch->cache workflows.

## Status
Open
