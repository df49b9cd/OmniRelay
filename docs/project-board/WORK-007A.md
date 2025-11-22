# WORK-007A – CA Bootstrap & CSR Issuance

## Goal
Deliver MeshKit CA service endpoints for CSR submission and certificate issuance.

## Scope
- CA key management (backed by KMS/HSM if available).
- CSR API, validation rules, issuance pipeline.
- mTLS enforcement on CA endpoints.

## Acceptance Criteria
- CSR -> issued cert flow works in integration tests.
- Keys stored securely; audit entry recorded for issuance.

## Status
Open
