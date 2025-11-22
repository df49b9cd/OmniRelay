# WORK-012B – Health Rules & SLO Correlation

## Goal
Define health evaluation rules that map telemetry to SLO/regression signals keyed by config epoch and rollout stage.

## Scope
- Rule set for error/latency thresholds; tie to rollout stages.
- Outputs consumed by WORK-011 gates.

## Acceptance Criteria
- Regression in synthetic tests triggers health signals consumed by rollout manager.

## Status
Open
