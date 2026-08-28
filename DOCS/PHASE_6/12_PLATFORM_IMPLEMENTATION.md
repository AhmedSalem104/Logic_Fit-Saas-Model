# Phase 6 Platform Foundation Implementation

**Status:** Implemented; final browser-adapter verification remains external
to the LogicFit runtime.

## Scope

This implementation is the bounded, read-only Platform Foundation approved
by the Phase 6 contracts. It contains five vertical slices:

1. Platform overview;
2. organization registry reads;
3. Gym registry reads;
4. Gym database registry reads;
5. request-time monitoring snapshot.

No provisioning, mutation, billing, settings API, feature-flag API, server
automation, Gym operational reads, or Flutter Platform Admin UI was added.

## Architecture

- `LogicFit.Api` exposes the HTTP boundary.
- `LogicFit.Application` owns the platform use-case contract and
  authorization decision.
- `LogicFit.Infrastructure` owns the SQL Server Control Plane projections.
- `platform.organizations`, `platform.gyms`, and
  `platform.gym_databases` are the only platform registry sources.
- The existing single `audit.events` table was extended with nullable
  `scope_type` and `scope_id` through one additive EF Core migration.
- No Gym database query is made by a Platform Foundation endpoint.

## Security boundary

Every admitted endpoint requires an authenticated, MFA-verified platform
session with the existing `platform.view` permission. A session carrying a
Gym scope is denied server-side. The Web permission check is only a UX aid;
the application service is authoritative.

Responses use the existing LogicFit envelope and never project
`connection_secret_ref` or any credential/connection-string field.

## Implementation map

| Slice | Application | Infrastructure | API | Web | Test |
|---|---|---|---|---|---|
| Overview | `IPlatformFoundationService.GetOverviewAsync` | registry counts | `/platform/overview` | `PA-W-001` | authenticated/unauthenticated/permission and response tests |
| Organizations | list/detail service methods | `SqlPlatformRepository` | `/platform/organizations` and `/{organizationId}` | `PA-W-002` | filters, paging, not-found, redaction |
| Gyms | list/detail service methods | `SqlPlatformRepository` | `/gyms` and `/{gymId}` | `PA-W-002`, `PA-W-003` | scope, filters, not-found |
| Databases | list/detail service methods | `SqlPlatformRepository` | `/platform/databases` and `/{databaseId}` | `PA-W-003`, `PA-W-005` | paging, filters, redaction |
| Monitoring | snapshot service method | registered database projection | `/platform/monitoring` | `PA-W-001`, `PA-W-009` | source/scope and safe response |

## Phase boundaries preserved

Phase 7 still owns provisioning, database creation, placement execution,
new-Gym migration orchestration, and provisioning lifecycle execution.
Phase 8 still owns Members and member operational data.
