# WORK-008B – Control Stream Client (Resume/Backoff)

## Goal
Implement control-plane subscription with resume tokens and backoff, without participating in leader election.

## Scope
- gRPC client for deltas/snapshots; resume token handling.
- Backoff policy with metrics/logs.

## Acceptance Criteria
- Disconnect/reconnect tested; no missed epochs; backoff observable.

## Status
Open
