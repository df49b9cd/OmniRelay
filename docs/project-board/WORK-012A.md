# WORK-012A – Telemetry Tagging & Ingest Pipeline

## Goal
Ingest OTLP metrics/logs/traces and ensure each record is tagged with node ID, capability set, and config epoch/stage.

## Scope
- Configure collector/processor; enforce tagging; reject or downgrade untagged records.
- Rate limits and buffering.

## Acceptance Criteria
- Tagged telemetry observed in storage; untagged records handled per policy.
- Load test shows stable ingest with backpressure behavior documented.

## Status
Open
