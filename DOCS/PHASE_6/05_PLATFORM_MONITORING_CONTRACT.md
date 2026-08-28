# Phase 6 Platform Monitoring Contract

**Status:** GREEN — bounded snapshot contract

## Scope

Phase 6 provides Platform Admin request-time health/status metadata only. It
does not introduce real-time monitoring infrastructure, metric storage,
Prometheus, Grafana, Redis, external monitoring, queues, or alert workers.

## Metrics and sources

| Field/metric | Source | Database/query responsibility | Freshness/failure |
|---|---|---|---|
| API health/status/version/environment | Existing LogicFit health/version infrastructure | API health service | Snapshot at request time; safe 503 on dependency failure |
| Organization total | Control Plane `platform.organizations` | Platform overview query | Snapshot at request time |
| Gym total/status counts | Control Plane `platform.gyms` | Server-side grouped query | Snapshot at request time |
| Database total/status rows | Control Plane `platform.gym_databases` | Server-side grouped query | Snapshot at request time |
| Database schema/seed/last-health metadata | Control Plane `platform.gym_databases` | Server-side safe projection | Stored metadata timestamp; no credential output |

## Explicitly deferred monitoring metrics

Latency percentiles, HTTP 5xx/401/403 windows, backup failures, migration
failures, provisioning failures, storage failures, queues, and alert
threshold evaluation remain Platform Operations scope. Their Phase 2 policy
thresholds are preserved but are not implemented or exposed as Phase 6
metrics.

## API response

`GET /api/v1/platform/overview` returns `PlatformOverview`.
`GET /api/v1/platform/monitoring` returns `PlatformMonitoringSnapshot` as
defined in `03_PLATFORM_API_CONTRACT.md`.

Both require authenticated Platform scope and existing `platform.view`.
They never read Gym operational tables. A Control Plane failure returns the
existing safe `503 DEPENDENCY_UNAVAILABLE` envelope; partial data is not
reported as a successful complete snapshot.
