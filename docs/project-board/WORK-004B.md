# WORK-004B – Sidecar Container Hardening

## Goal
Produce hardened sidecar image (non-root, readonly FS) with health endpoints.

## Scope
- Container build with slim base, non-root user, readonly FS, drop caps.
- Health/readiness endpoints wired.
- Publish per-RID images with capability manifest.

## Acceptance Criteria
- Image passes container security scan; runs with readonly FS and non-root.
- Health endpoints respond; capability manifest present.

## Status
Open
