# WORK-019C – Runtime/Container Hardening & Vulnerability Scanning

## Goal
Harden runtime defaults (non-root, readonly FS, minimal perms) and integrate vulnerability scanning.

## Scope
- Hardened container specs; seccomp/AppArmor where applicable.
- Dependency scanning and CVE policy in CI.

## Acceptance Criteria
- Containers run non-root/readonly; security scans clean or waivers documented.

## Status
Open
