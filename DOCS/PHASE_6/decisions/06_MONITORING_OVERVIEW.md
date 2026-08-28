# P6-D-006 — Monitoring and Platform Overview Sources

**Status:** APPROVED

## Problem

The broad Platform catalog mentions operational metrics whose source,
freshness, and aggregation are not available in the current foundation.

## Existing evidence

The API already has health/version infrastructure. The Control Plane has
organization, Gym, and database registry metadata. The approval disallows
new real-time monitoring infrastructure and detailed Gym operational data in
the Platform overview.

## Options

1. Define a request-time snapshot from existing API health and Control Plane
   registry rows.
2. Add persistent operational metric storage and external monitoring.
3. Defer all overview/monitoring views.

## Recommendation

**Selected: Option 1.** Phase 6 exposes only API health/version/environment,
organization totals, Gym status counts, database status counts, and safe
database schema/seed/health metadata. Each value is a request-time snapshot
with a documented source.

## Impact

No Prometheus, Grafana, Redis, queue, alert worker, latency window, backup
metric, provisioning metric, or real-time stream is introduced. A Control
Plane dependency failure returns the safe dependency-unavailable response;
partial data is not represented as a complete successful snapshot.

## Affected surfaces

- **DB:** Existing registry tables; no metric store.
- **API:** Overview and monitoring DTOs/routes in `03_PLATFORM_API_CONTRACT.md`.
- **Permissions:** Existing `platform.view`.
- **Web:** `PA-W-001` and `PA-W-009` snapshot views.
- **Flutter:** No monitoring UI.
- **Tests:** Source, scope, freshness, failure, redaction, and permission
  tests.
