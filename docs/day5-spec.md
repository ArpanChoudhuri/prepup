# Day 5 — Spec: Multi-tenant Notifications & Traveler Check-ins

## Problem
Provide multi-tenant APIs to send time-sensitive notifications (SMS/email/push) and record traveler check-ins with low latency, high reliability, and clear cost controls.

## Functional
- POST /v1/notifications (enqueue/send)
- GET /v1/notifications/{id}/status
- POST /v1/checkins (dedupe)
- Idempotency via `Idempotency-Key`
- Per-tenant API keys + quotas
- Audit trail
- (Optional) provider webhooks

## Non-Functional
- P99 enqueue < 150ms; P99 dispatch < 5s
- Scale: 5k RPS ingress; 150k/min burst
- Availability ≥ 99.9%
- Queue-backed; exactly-once effects via idempotency
- Cost target: <$2 / 1M (ex-carrier)

## Assumptions
- SMS: SNS; Email: SES; Push: FCM/APNs
- Single region initially; KMS at rest
- Status TTL 30d in Dynamo → archive to S3

## Success Metrics
- Delivery %, SLO attainment %, cost/1k, throttles/tenant, DLQ depth/age

## Out of Scope
- Templates UI; active-active multi-region; real-time streaming (poll first)
