# WORK-006B – Watch Streams (Deltas/Snapshots) with Resume/Backoff

## Goal
Implement control watch streams over gRPC supporting deltas, snapshots, resume tokens, and backoff guidance.

## Scope
- Server streaming endpoints; client logic for subscribe/resume.
- Backoff policy and retry limits; LKG fallback signaling.

## Acceptance Criteria
- Integration tests cover connect/drop/resume without missing epochs.
- Backoff behavior observable via metrics/logs.

## Status
Open
