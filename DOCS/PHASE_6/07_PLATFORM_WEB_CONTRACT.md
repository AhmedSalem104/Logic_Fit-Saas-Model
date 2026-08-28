# Phase 6 Platform Web Contract

**Status:** GREEN — read-only Platform Admin screen contract

## Route namespace

The approved Platform Admin Web root is `/platform-admin`; the Gym
application root remains `/app`. The API namespace remains the existing
`/api/v1/platform/...` and `/api/v1/gyms/...` routes. No second API namespace
or compatibility route is introduced.

The Phase 2 `PA-W-*` screen IDs remain traceability identifiers. For Phase 6,
their Platform Admin Web routes are normalized under `/platform-admin`.

## Admitted screens

| Screen ID | Canonical route | API dependencies | Permission | Actions |
|---|---|---|---|---|
| `PA-W-001` | `/platform-admin` | overview, monitoring | `platform.view` | Read cards; refresh; no mutation |
| `PA-W-002` | `/platform-admin/organizations` | organizations list, gyms list | `platform.view` | Search/filter/paginate/open; no create/status mutation |
| `PA-W-003` | `/platform-admin/gyms/:gymId` | Gym detail, database detail/list | `platform.view` | Read identity/status/placement metadata; no activate/deactivate |
| `PA-W-005` | `/platform-admin/databases` | database list/detail | `platform.view` | Read/filter/open; no migration/backup action |
| `PA-W-009` | `/platform-admin/operations` | monitoring snapshot | `platform.view` | Read health/registry snapshot; audit panel deferred |

## Screen data contract

- `PA-W-001` uses `PlatformOverview` and `PlatformMonitoringSnapshot`.
- `PA-W-002` uses `OrganizationSummary[]` and `GymSummary[]`.
- `PA-W-003` uses `GymSummary` and safe `DatabaseRegistrySummary[]`.
- `PA-W-005` uses paged `DatabaseRegistrySummary[]`.
- `PA-W-009` uses `PlatformMonitoringSnapshot` only.

All use the standard loading, empty, error, success, and retry states. Search,
filter, sort, and pagination are server-side. No screen displays connection
secret references or Gym operational records. Backend responses, not UI
hiding, determine authorization.

## UI behavior

All admitted screens use the existing React design system and support Arabic,
RTL, responsive layouts, light/dark themes, keyboard-accessible navigation,
and safe API error/request-ID display. No form, mutation drawer, optimistic
write, or client-side status transition is included in this Phase 6 contract.

## Deferred screens

- `PA-W-004`: Phase 7 provisioning.
- `PA-W-006`: later Platform Operations backup/restore.
- `PA-W-007`: Phase 5B access administration; not reimplemented.
- `PA-W-008`: settings/flags not admitted in Phase 6.
