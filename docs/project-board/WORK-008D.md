# WORK-008D – Resource/Security Hardening & Footprint

## Goal
Keep the agent lightweight and least-privilege while quantifying its resource footprint.

## Scope
- Run as non-root with minimal FS permissions; sandboxed paths.
- Measure CPU/memory under load; document limits.
- Add startup flags for resource caps if applicable.

## Acceptance Criteria
- Footprint measurements published; meets target ceilings.
- Security posture documented and validated in tests.

## Status
Open
