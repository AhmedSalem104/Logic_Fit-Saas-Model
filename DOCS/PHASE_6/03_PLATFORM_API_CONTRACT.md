# Phase 6 Platform API Contract

**Status:** GREEN — read-only contract closed; implementation not started
**Canonical base:** `/api/v1`

Phase 6 admits a bounded read-only Platform Foundation API. No mutation API
is admitted because the existing canonical 15-permission catalog has no
approved organization/server/database/settings/flag mutation permission.

## Shared API rules

- Authentication: valid SQL-backed LogicFit session.
- Authorization: exact existing permission `platform.view`.
- Scope: authenticated Platform scope; target Gym IDs are independently
  validated as Control Plane registry resources.
- Data source: Control Plane only, except existing health infrastructure.
- Success/error envelopes, request IDs, pagination, filtering, and safe
  errors come from `DOCS/PHASE_2/03_API_CONTRACT.md`.
- No raw connection references, credentials, private keys, or Gym
  operational data appear in responses.
- GET operations are safe/idempotent and do not require `Idempotency-Key`.
- Read-only success does not create a business audit mutation; denied or
  suspicious access continues through the existing security audit/logging
  boundary.

## JSON scalar conventions

- IDs are UUID strings.
- UTC instants are ISO-8601 strings with a UTC offset.
- `status` values are source registry values; Phase 6 introduces no new
  status labels beyond the approved Active/Inactive minimum for future
  lifecycle mutation.
- Nullable database metadata is represented as JSON `null`.

## Canonical DTOs

### `OrganizationSummary`

```json
{
  "organizationId": "uuid",
  "name": "string",
  "slug": "string",
  "status": "string",
  "createdAtUtc": "datetime",
  "updatedAtUtc": "datetime"
}
```

No plan/subscription fields are present.

### `GymSummary`

```json
{
  "gymId": "uuid",
  "organizationId": "uuid",
  "name": "string",
  "slug": "string",
  "status": "string",
  "timezoneName": "string",
  "createdAtUtc": "datetime",
  "updatedAtUtc": "datetime"
}
```

### `DatabaseRegistrySummary`

```json
{
  "gymDatabaseId": "uuid",
  "gymId": "uuid",
  "databaseName": "string",
  "environment": "string",
  "schemaVersion": "string|null",
  "seedVersion": "string|null",
  "status": "string",
  "lastHealthAtUtc": "datetime|null"
}
```

`connectionSecretRef` is excluded.

### `StatusCount`

```json
{ "status": "string", "count": 0 }
```

### `PlatformOverview`

```json
{
  "observedAtUtc": "datetime",
  "platformHealth": {
    "status": "string",
    "service": "api",
    "version": "string",
    "environment": "string"
  },
  "organizationCount": 0,
  "gymCounts": { "total": 0, "byStatus": [] },
  "databaseCounts": { "total": 0, "byStatus": [] }
}
```

Counts are Control Plane row counts grouped by stored status. They never
count members, payments, attendance, plans, nutrition, store, CRM, or other
Gym operational records.

### `PlatformMonitoringSnapshot`

```json
{
  "observedAtUtc": "datetime",
  "platformHealth": {
    "status": "string",
    "service": "api",
    "version": "string",
    "environment": "string"
  },
  "registeredDatabases": []
}
```

`registeredDatabases` contains `DatabaseRegistrySummary` values. This is a
request-time registry/health snapshot, not a real-time monitoring stream.

## Endpoint catalog for Phase 6

### `GET /api/v1/platform/overview`

- **Auth/permission:** authenticated Platform scope + `platform.view`.
- **Request:** no body; no query parameters.
- **Response:** `data: PlatformOverview` in the standard success envelope.
- **Sources:** `platform.organizations`, `platform.gyms`,
  `platform.gym_databases`, existing API health/version infrastructure.
- **Errors:** `401 AUTHENTICATION_REQUIRED`; `403 PERMISSION_DENIED`; `503
  DEPENDENCY_UNAVAILABLE` if the Control Plane cannot be read.
- **Audit:** no mutation event; access denial uses existing security audit.

### `GET /api/v1/platform/organizations`

- **Auth/permission:** authenticated Platform scope + `platform.view`.
- **Query:** `page` (default 1), `pageSize` (default 25, max 100), optional
  `search`, `status`, and `sort` using only `name`, `slug`,
  `createdAtUtc`, or `updatedAtUtc`, with `asc|desc`.
- **Response:** paged `OrganizationSummary[]`.
- **Errors:** `400 INVALID_FILTER` for unsupported filters/sorts; `401`;
  `403`; `503 DEPENDENCY_UNAVAILABLE`.
- **Audit:** no mutation event.

### `GET /api/v1/platform/organizations/{organizationId}`

- **Auth/permission:** authenticated Platform scope + `platform.view`.
- **Request:** no body/query.
- **Response:** `OrganizationSummary`.
- **Errors:** `401`; `403`; `404 RESOURCE_NOT_FOUND` without cross-scope
  existence leakage; `503`.

### `GET /api/v1/gyms`

- **Auth/permission:** authenticated Platform scope + `platform.view`.
- **Query:** `page`, `pageSize`, optional `organizationId` UUID, `search`,
  `status`, and `sort` using `name`, `slug`, `createdAtUtc`, or
  `updatedAtUtc`, with `asc|desc`.
- **Response:** paged `GymSummary[]`.
- **Errors:** `400 INVALID_FILTER`; `401`; `403`; `503`.

### `GET /api/v1/gyms/{gymId}`

- **Auth/permission:** authenticated Platform scope + `platform.view`.
- **Request:** no body/query.
- **Response:** `GymSummary` plus a safe `databases` array of
  `DatabaseRegistrySummary` for that Gym.
- **Errors:** `401`; `403`; `404`; `503`.
- **Security:** no Gym operational query is executed.

### `GET /api/v1/platform/databases`

- **Auth/permission:** authenticated Platform scope + `platform.view`.
- **Query:** `page`, `pageSize`, optional `gymId` UUID, `environment`,
  `status`, and `sort` using `databaseName`, `status`, or
  `lastHealthAtUtc`, with `asc|desc`.
- **Response:** paged `DatabaseRegistrySummary[]`.
- **Errors:** `400 INVALID_FILTER`; `401`; `403`; `503`.

### `GET /api/v1/platform/databases/{databaseId}`

- **Auth/permission:** authenticated Platform scope + `platform.view`.
- **Request:** no body/query.
- **Response:** `DatabaseRegistrySummary`.
- **Errors:** `401`; `403`; `404`; `503`.

### `GET /api/v1/platform/monitoring`

- **Auth/permission:** authenticated Platform scope + `platform.view`.
- **Request:** no body/query; no realtime/window parameters.
- **Response:** `PlatformMonitoringSnapshot`.
- **Sources:** existing API health/version and Control Plane registry rows.
- **Errors:** `401`; `403`; `503 DEPENDENCY_UNAVAILABLE`.

## Deferred routes

The following Phase 2 routes are explicitly outside the Phase 6 API list:

- all POST/PATCH organization/Gym/server/settings/flag operations;
- `/api/v1/platform/servers*` until placement/provisioning owns a consumer;
- `/api/v1/platform/audit` because `platform.audit.view` is not in the
  canonical 15-key catalog;
- `/api/v1/platform/feature-flags/{key}` and
  `/api/v1/platform/settings/{key}` because no approved keys or permissions
  exist;
- provisioning, migration execution, backup/restore, deployment, and DR
  operations because they belong to Phase 7/later Platform Operations.

There is one canonical API route per admitted operation and no compatibility
duplicate route.
