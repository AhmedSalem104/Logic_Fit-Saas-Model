# Phase 6 Platform API Implementation

All routes use `/api/v1/`, the existing LogicFit response/error envelope,
request IDs, JSON structured access logging, the `platform` scope, and the
existing `platform.view` permission.

| Method | Route | Success | Source | Notes |
|---|---|---:|---|---|
| GET | `/api/v1/platform/overview` | 200 | Control Plane registry counts + API runtime health | platform-only aggregate |
| GET | `/api/v1/platform/organizations` | 200 | `platform.organizations` | page/pageSize/search/status/sort |
| GET | `/api/v1/platform/organizations/{organizationId}` | 200 | `platform.organizations` | `RESOURCE_NOT_FOUND` when absent |
| GET | `/api/v1/gyms` | 200 | `platform.gyms` | page/pageSize/organizationId/search/status/sort |
| GET | `/api/v1/gyms/{gymId}` | 200 | `platform.gyms` + registry metadata | detail contains safe database metadata |
| GET | `/api/v1/platform/databases` | 200 | `platform.gym_databases` | page/pageSize/gymId/environment/status/sort |
| GET | `/api/v1/platform/databases/{databaseId}` | 200 | `platform.gym_databases` | secret reference is excluded |
| GET | `/api/v1/platform/monitoring` | 200 | API runtime health + `platform.gym_databases` | request-time snapshot, not realtime monitoring |

## Common behavior

- No request body is accepted by these read-only operations.
- Unauthenticated requests return `401 AUTHENTICATION_REQUIRED`.
- Authenticated users without `platform.view`, including Gym-scoped users,
  return `403 PERMISSION_DENIED`.
- Non-platform/MFA-incomplete sessions are denied before registry access.
- Invalid page, page size, or sort values return `400 INVALID_FILTER`.
- Missing detail resources return `404 RESOURCE_NOT_FOUND`.
- Control Plane dependency failures return sanitized `503
  DEPENDENCY_UNAVAILABLE`.
- Collections default to page 1 and page size 25 and cap page size at 100;
  response metadata contains page, pageSize, total, and hasNext.
- Stable ID tie-breakers make sorting deterministic.
- All database projections use `AsNoTracking` and select only contract
  fields.

## Runtime authorization

`PlatformFoundationService.AuthorizePlatformAsync` checks the current
authenticated user, MFA state, platform scope, and `platform.view` through
the existing authentication service. Permission-denial events use the
existing audit repository and identify the platform scope without secrets.

## Client contract

React calls these routes through `apps/web/src/lib/api.ts`. There is no
direct SQL Server access and no alternative endpoint. The Phase 6 Flutter
contract explicitly requires no Platform Admin mobile UI.
