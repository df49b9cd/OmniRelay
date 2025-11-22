# WORK-008C – Telemetry Forwarding with Buffering/Backpressure

## Goal
Forward telemetry (OTLP) from nodes with bounded buffers and backpressure handling.

## Scope
- Buffering strategy, drop policies, and rate limits.
- Metrics for queue depth, drops, latency.

## Acceptance Criteria
- Under ingest backpressure, data loss bounded and observable; system remains responsive.

## Status
Open
